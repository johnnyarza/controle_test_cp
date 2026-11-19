using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ProbetaControl.Data;
using ProbetaControl.Models;

namespace ProbetaControl.Services
{
    /// <summary>
    /// Photos of a probeta (phase 9 decision): any signed-in user adds, replaces or removes them, also on a locked
    /// ensayo (a photo doesn't change any result). Each one is turned upright (phone EXIF), resized to at most
    /// 1600 px and saved as JPEG; at most <see cref="MaxPerSpecimen"/> per probeta. Changes are saved at once.
    /// </summary>
    public sealed class SpecimenPhotoService
    {
        public const int MaxPerSpecimen = 3;
        public const int MaxSidePx = 1600;

        /// <summary>Phone photos are 3–12 MB; anything bigger is not a photo.</summary>
        public const int MaxSourceBytes = 40 * 1024 * 1024;

        public const string FileFilter = "Fotos JPG o PNG (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

        public const string NotAnImage = "El archivo no es una foto que el programa pueda leer. Use una foto JPG o PNG.";

        private const long JpegQuality = 85;

        /// <summary>EXIF orientation tag written by phones.</summary>
        private const int OrientationTag = 0x0112;

        private readonly SpecimenPhotoRepository _repository;
        private readonly AppSession _session;

        public SpecimenPhotoService(SpecimenPhotoRepository repository, AppSession session)
        {
            _repository = repository;
            _session = session;
        }

        public Task<IReadOnlyList<SpecimenPhoto>> ListAsync(int specimenId, CancellationToken cancellationToken = default) =>
            _repository.ListAsync(specimenId, cancellationToken);

        /// <summary>Adds a photo from a file's bytes (or replaces <paramref name="replaceId"/>). Returns the saved photo.</summary>
        public async Task<SpecimenPhoto> AddAsync(int specimenId, byte[] source, int? replaceId = null, CancellationToken cancellationToken = default)
        {
            var userId = _session.UserId ?? throw new AccessDeniedException("Inicie sesión para agregar fotos.");
            var prepared = await Task.Run(() => Prepare(source), cancellationToken).ConfigureAwait(false);
            var photo = new SpecimenPhoto
            {
                SpecimenId = specimenId,
                Photo = prepared.Jpeg,
                ContentType = "image/jpeg",
                WidthPx = prepared.Width,
                HeightPx = prepared.Height,
                CreatedBy = userId,
                CreatedByName = _session.CurrentUser.DisplayName,
            };

            await _repository.AddAsync(photo, replaceId, MaxPerSpecimen, cancellationToken).ConfigureAwait(false);
            return photo;
        }

        /// <summary>Really deletes the photo, with an audit row of who, when and its size.</summary>
        public Task DeleteAsync(int photoId, CancellationToken cancellationToken = default)
        {
            if (_session.UserId == null)
            {
                throw new AccessDeniedException("Inicie sesión para quitar fotos.");
            }

            return _repository.DeleteAsync(photoId, cancellationToken);
        }

        /// <summary>"Probeta 2297 - foto 1.jpg".</summary>
        public static string FileName(int specimenId, int position) => $"Probeta {specimenId} - foto {position}.jpg";

        /// <summary>Upright, at most <see cref="MaxSidePx"/> on its longer side, JPEG. Throws <see cref="ValidationException"/> for anything else.</summary>
        public static (byte[] Jpeg, int Width, int Height) Prepare(byte[] source)
        {
            if (source == null || source.Length == 0)
            {
                throw new ValidationException("Photo", NotAnImage);
            }

            if (source.Length > MaxSourceBytes)
            {
                throw new ValidationException("Photo", "El archivo es demasiado grande para ser una foto (más de 40 MB).");
            }

            try
            {
                using (var stream = new MemoryStream(source))
                using (var original = Image.FromStream(stream, true, true))
                {
                    Rotate(original);
                    var scale = Math.Min(1.0, (double)MaxSidePx / Math.Max(original.Width, original.Height));
                    var width = Math.Max(1, (int)Math.Round(original.Width * scale));
                    var height = Math.Max(1, (int)Math.Round(original.Height * scale));

                    using (var resized = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                    {
                        resized.SetResolution(96, 96);
                        using (var g = Graphics.FromImage(resized))
                        {
                            // A transparent PNG gets a white background, as on paper.
                            g.Clear(Color.White);
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            using (var attributes = new ImageAttributes())
                            {
                                attributes.SetWrapMode(WrapMode.TileFlipXY);
                                g.DrawImage(original, new Rectangle(0, 0, width, height), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                            }
                        }

                        return (Jpeg(resized), width, height);
                    }
                }
            }
            catch (ArgumentException)
            {
                throw new ValidationException("Photo", NotAnImage);
            }
            catch (OutOfMemoryException)
            {
                // What GDI+ throws for a file it can't decode.
                throw new ValidationException("Photo", NotAnImage);
            }
            catch (ExternalException)
            {
                throw new ValidationException("Photo", NotAnImage);
            }
        }

        /// <summary>Turns the image the way the phone's EXIF orientation says (the pixels are stored as the sensor saw them).</summary>
        private static void Rotate(Image image)
        {
            if (!image.PropertyIdList.Contains(OrientationTag))
            {
                return;
            }

            var value = image.GetPropertyItem(OrientationTag).Value;
            var orientation = value != null && value.Length > 0 ? value[0] : 1;
            RotateFlipType? flip;
            switch (orientation)
            {
                case 2: flip = RotateFlipType.RotateNoneFlipX; break;
                case 3: flip = RotateFlipType.Rotate180FlipNone; break;
                case 4: flip = RotateFlipType.Rotate180FlipX; break;
                case 5: flip = RotateFlipType.Rotate90FlipX; break;
                case 6: flip = RotateFlipType.Rotate90FlipNone; break;
                case 7: flip = RotateFlipType.Rotate270FlipX; break;
                case 8: flip = RotateFlipType.Rotate270FlipNone; break;
                default: flip = null; break;
            }

            if (flip.HasValue)
            {
                image.RotateFlip(flip.Value);
            }
        }

        private static byte[] Jpeg(Image image)
        {
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using (var parameters = new EncoderParameters(1))
            using (var output = new MemoryStream())
            {
                parameters.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
                image.Save(output, codec, parameters);
                return output.ToArray();
            }
        }
    }
}
