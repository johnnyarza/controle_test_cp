using System.Threading;

namespace ProbetaControl.Tests.Reports
{
    /// <summary>
    /// Probeta codes for tests that read QR codes back from drawn labels.
    /// <para>
    /// Every QR the program draws is valid (<see cref="LabelLayoutTests.Every_code_makes_a_valid_QR"/>), but for about 1 code
    /// in 450 the standard mask choice (QRCoder's and ZXing's own alike) gives a pattern ZXing's image detector misses at
    /// most sizes; the same text with any other mask reads at every size. With random codes, label tests then failed now
    /// and then (seen 2026-11-20 with 6AD9DA5DD716C5C4 on the 2 × 8, 105 × 37 mm sheet). These 40 were picked (seeded
    /// random, 2 of 42 rejected) because each reads at every size from 120 to 420 px and first and last on every sheet
    /// preset, so the tests are the same on every run. Making every printed label read with ZXing-based scanner apps is
    /// left for phase 9 (docs/decisions.md).
    /// </para>
    /// </summary>
    internal static class ReadableCodes
    {
        public static readonly string[] All =
        {
            "8B9D647D6D8A865E", "CE62136384F56C11", "718583E55D7DACC1", "7147B2A27B18CD81", "6D87DDE1DF4FDBC2",
            "61191F7CAB58AC4D", "69B14DAFAFDAA50D", "F0A30C26D7E4E3B5", "9592F6FC188A3222", "A9C183435E2F1618",
            "9A6538C9CA1DD292", "E1CFF03C42B4EB7D", "3569E71862AF34DE", "7F2B06ED5662AEC1", "C0631A4899F5E0F4",
            "D6060DF548FEB5EA", "305A499F17E43C7F", "212FAF4D2979D11B", "7E6F57FF95EC448A", "4B11174F54B9788D",
            "3EA04051D14B60DC", "35F276D9A6B65544", "15BF5406CCEC2A67", "B7210E8938804A33", "0E7BD8FAC4DA43B9",
            "46BB1A62429FE788", "4AFC3B12F75A0F3A", "70BFB324834124B5", "A843CA2BDD5B7F3B", "7B9BBF11D2C72517",
            "3A863E3C44373BB4", "6B9E8905271F3604", "9665BAFFD5BBAAEF", "7C00975C9AFE67A8", "9A9BE9FC03E77070",
            "0716C03FD27A145B", "64BB7A6D0D26427F", "DB4FC044A0DF196C", "36E06A8B353901AF", "F44AF7BEFA6B1C01",
        };

        private static int _next = -1;

        /// <summary>The list in turn, for probetas saved to a test database (PublicCode is unique there).</summary>
        public static string Next() => All[Interlocked.Increment(ref _next) % All.Length];
    }
}
