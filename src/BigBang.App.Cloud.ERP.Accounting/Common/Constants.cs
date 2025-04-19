namespace BigBang.App.Cloud.ERP.Accounting.Common
{
    public static class Constants
    {
        public const string AccountTreePrefix = "AccountTree_";
        public const string VoucherTemplateEnumName = "Template";

        public const string LeftToRightMark = "\u200e";

        public const string MobileNumberRegex = @"^09\d{9}$";
        public const string CardNumberRegex = @"^\d{16}$";
        public const string ShebaNumberRegex = @"^\d{24}$";
        public const string VoucherTemplateTitleFormatRegex = @"#(.*?)#";
        public const string AlphabetRegex = @"^[A-Za-z]+(?: [A-Za-z]+)*$";

        public const long PodBusinessId = 1;

        public const string UserInfoBigBangPhone = "BBPhone";
        public const string UserInfoFirstName = "FirstName";
        public const string UserInfoLastName = "LastName";
        public const string UserInfoBusinessName = "BusinessName";

        public const string OwnerIdClaimType = " OwnerId";
        public const string BusinessIdClaimType = "BusinessId";
        public const string FiscalPeriodIdClaimType = "FiscalPeriodId";

        public static byte[] PdfHeader = [0x25, 0x50, 0x44, 0x46];
        public static byte[] JpgHeader = [0xFF, 0xD8, 0xFF];
        public static byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        public static byte[] GifHeader = [0x47, 0x49, 0x46, 0x38];

        public const string InvoiceReport = "InvoiceReport";
        public static string[] GrossProfitAndLossSortBy = ["SellQuantity", "SellAmount", "CostQuantity", "CostAmount", "DifferenceAmount"];
        public static string[] DetailedAccountFinancialBalanceSortBy = ["TotalDebit", "TotalCredit", "Difference"];
    }
}