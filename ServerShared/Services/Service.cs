// namespace FileFlows.ServerShared.Services;
//
// /// <summary>
// /// A service lets you communicate with the FileFlows server
// /// </summary>
// public class Service
// {
//     private static string _ServiceBaseUrl;
//     /// <summary>
//     /// Gets or sets the Base URL of the FileFlows server
//     /// </summary>
//     public static string ServiceBaseUrl 
//     { 
//         get => _ServiceBaseUrl;
//         set
//         {
//             if(value == null)
//             {
//                 _ServiceBaseUrl = string.Empty;
//                 return;
//             }
//             if(value.EndsWith("/"))
//                 _ServiceBaseUrl = value.Substring(0, value.Length - 1); 
//             else
//                 _ServiceBaseUrl = value;
//         }
//     }
//
//     
// }
