namespace Plugins.ODDB.Scripts.Editor.Utils
{
    public struct OnGoogleSheetDataRequest
    {
        public static readonly OnGoogleSheetDataRequest Success = new OnGoogleSheetDataRequest
        {
            IsSuccess = true,
            Message = "Request succeeded",
            Response = null
        };
        
        public static readonly OnGoogleSheetDataRequest Failed = new OnGoogleSheetDataRequest
        {
            IsSuccess = false,
            Message = "Request failed",
            Response = null
        };
        
        public bool IsSuccess;
        public string Message;
        public GoogleSheetsResponse Response;
    }
}