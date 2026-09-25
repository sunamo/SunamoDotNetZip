namespace Ionic.Zip;

    partial class ZipFile
    {
        private static System.Text.Encoding _defaultEncoding = null;
        private static bool _defaultEncodingInitialized = false;
        // Static constructor for ZipFile
        // Code Pages 437 and 1252 for English are same
        // Code Page 1252 Windows Latin 1 (ANSI) - https://msdn.microsoft.com/en-us/library/cc195054.aspx
        // Code Page 437 MS-DOS Latin US - https://msdn.microsoft.com/en-us/library/cc195060.aspx
        static ZipFile()
        {
            System.Text.Encoding ibm437 = null;
            try
            {
                ibm437 = System.Text.Encoding.GetEncoding("IBM437");
            }
            catch (Exception /*e*/)
            {
            }
#if NETCOREAPP2_0 || NETSTANDARD2_0
            if (ibm437 == null)
            {
                try
                {
                    ibm437 = System.Text.CodePagesEncodingProvider.Instance.GetEncoding(1252);
                }
                catch (Exception /*e*/)
                {
                }
            }
#else
            if (ibm437 == null)
            {
                try
                {
                    ibm437 = System.Text.Encoding.GetEncoding(1252);
                }
                catch (Exception /*e*/)
                {
                }
            }
#endif
            _defaultEncoding = ibm437;
        }
        public static System.Text.Encoding DefaultEncoding
        {
            get
            {
                return _defaultEncoding;
            }
            set
            {
                if (_defaultEncodingInitialized)
                {
                    return;
                }
                _defaultEncoding = value;
                _defaultEncodingInitialized = true;
            }
        }
    }
