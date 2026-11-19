using System;
using System.Collections.Generic;

namespace ProbetaControl.Models
{
    /// <summary>A photo of a probeta (table SpecimenPhotos, script 014). Always a JPEG resized by the program.</summary>
    public sealed class SpecimenPhoto
    {
        public int Id { get; set; }

        public int SpecimenId { get; set; }

        /// <summary>The image file's bytes.</summary>
        public byte[] Photo { get; set; }

        public string ContentType { get; set; } = "image/jpeg";

        /// <summary>Worked out by MySQL from <see cref="Photo"/>.</summary>
        public int SizeBytes { get; set; }

        public int WidthPx { get; set; }

        public int HeightPx { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Users.Id of who added it.</summary>
        public int CreatedBy { get; set; }

        /// <summary>Full name (or user name) of who added it; read only.</summary>
        public string CreatedByName { get; set; }

        /// <summary>What the audit log keeps of a photo: everything but the image.</summary>
        public IReadOnlyDictionary<string, object> GetAuditValues() => new Dictionary<string, object>
        {
            ["SpecimenId"] = SpecimenId,
            ["ContentType"] = ContentType,
            ["SizeBytes"] = Photo?.Length ?? SizeBytes,
            ["WidthPx"] = WidthPx,
            ["HeightPx"] = HeightPx,
        };
    }
}
