namespace ProbetaControl.Data.Schema
{
    public static class SchemaInfo
    {
        /// <summary>
        /// Schema version this build of the app works with: the number of the last script in
        /// Database/Scripts. Raise it together with every new script (a test checks they match).
        /// </summary>
        public const int RequiredVersion = 11;
    }
}
