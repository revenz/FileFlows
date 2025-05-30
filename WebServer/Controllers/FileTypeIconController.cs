using Microsoft.AspNetCore.Mvc;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Icon file type controller
/// </summary>
[Route("/icon/filetype")]
[ApiExplorerSettings(IgnoreApi = true)]
public class FileTypeIconController : Controller
{
	private const string HEAD = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>
<svg height=""800px"" width=""800px"" version=""1.1"" id=""Capa_1"" xmlns=""http://www.w3.org/2000/svg"" 
	 viewBox=""0 0 56 56"" xml:space=""preserve"">
<style id=""text"">
  text {
    font-family: Helvetica, Arial;
	font-weight:500;
	fill:white;
	font-size: 13px;
  }
</style>
<g>
	<path style=""fill:#404040;"" d=""M36.985,0H7.963C7.155,0,6.5,0.655,6.5,1.926V55c0,0.345,0.655,1,1.463,1h40.074
		c0.808,0,1.463-0.655,1.463-1V12.978c0-0.696-0.093-0.92-0.257-1.085L37.607,0.257C37.442,0.093,37.218,0,36.985,0z""/>
	<polygon style=""fill:#666;"" points=""37.5,0.151 37.5,12 49.349,12"" />";

	private const string END = @"
</g>
</svg>";

	private const string AUDIO = "<path style=\"fill:#C8BDB8;\" d=\"M35.67,14.986c-0.567-0.796-1.3-1.543-2.308-2.351c-3.914-3.131-4.757-6.277-4.862-6.738V5\t\tc0-0.553-0.447-1-1-1s-1,0.447-1,1v1v8.359v9.053h-3.706c-3.882,0-6.294,1.961-6.294,5.117c0,3.466,2.24,5.706,5.706,5.706\t\tc3.471,0,6.294-2.823,6.294-6.294V16.468l0.298,0.243c0.34,0.336,0.861,0.72,1.521,1.205c2.318,1.709,6.2,4.567,5.224,7.793\t\tC35.514,25.807,35.5,25.904,35.5,26c0,0.43,0.278,0.826,0.71,0.957C36.307,26.986,36.404,27,36.5,27c0.43,0,0.826-0.278,0.957-0.71\t\tC39.084,20.915,37.035,16.9,35.67,14.986z M26.5,27.941c0,2.368-1.926,4.294-4.294,4.294c-2.355,0-3.706-1.351-3.706-3.706\t\tc0-2.576,2.335-3.117,4.294-3.117H26.5V27.941z M31.505,16.308c-0.571-0.422-1.065-0.785-1.371-1.081l-1.634-1.34v-3.473\t\tc0.827,1.174,1.987,2.483,3.612,3.783c0.858,0.688,1.472,1.308,1.929,1.95c0.716,1.003,1.431,2.339,1.788,3.978\t\tC34.502,18.515,32.745,17.221,31.505,16.308z\"/>";
	private const string ARCHIVE = "<g><path style=\"fill:#C8BDB8;\" d=\"M28.5,24v-2h2v-2h-2v-2h2v-2h-2v-2h2v-2h-2v-2h2V8h-2V6h-2v2h-2v2h2v2h-2v2h2v2h-2v2h2v2h-2v2h2v2\t\t\th-4v5c0,2.757,2.243,5,5,5s5-2.243,5-5v-5H28.5z M30.5,29c0,1.654-1.346,3-3,3s-3-1.346-3-3v-3h6V29z\"/>\t\t<path style=\"fill:#C8BDB8;\" d=\"M26.5,30h2c0.552,0,1-0.447,1-1s-0.448-1-1-1h-2c-0.552,0-1,0.447-1,1S25.948,30,26.5,30z\"/>\t</g>";
	private const string CODE = "<path style=\"fill:{COLOR};\" d=\"M15.5,24c-0.256,0-0.512-0.098-0.707-0.293c-0.391-0.391-0.391-1.023,0-1.414l6-6\t\tc0.391-0.391,1.023-0.391,1.414,0s0.391,1.023,0,1.414l-6,6C16.012,23.902,15.756,24,15.5,24z\"/>\t<path style=\"fill:{COLOR};\" d=\"M21.5,30c-0.256,0-0.512-0.098-0.707-0.293l-6-6c-0.391-0.391-0.391-1.023,0-1.414\t\ts1.023-0.391,1.414,0l6,6c0.391,0.391,0.391,1.023,0,1.414C22.012,29.902,21.756,30,21.5,30z\"/>\t<path style=\"fill:{COLOR};\" d=\"M33.5,30c-0.256,0-0.512-0.098-0.707-0.293c-0.391-0.391-0.391-1.023,0-1.414l6-6\t\tc0.391-0.391,1.023-0.391,1.414,0s0.391,1.023,0,1.414l-6,6C34.012,29.902,33.756,30,33.5,30z\"/>\t<path style=\"fill:{COLOR};\" d=\"M39.5,24c-0.256,0-0.512-0.098-0.707-0.293l-6-6c-0.391-0.391-0.391-1.023,0-1.414\t\ts1.023-0.391,1.414,0l6,6c0.391,0.391,0.391,1.023,0,1.414C40.012,23.902,39.756,24,39.5,24z\"/>\t<path style=\"fill:{COLOR};\" d=\"M24.5,32c-0.11,0-0.223-0.019-0.333-0.058c-0.521-0.184-0.794-0.755-0.61-1.276l6-17\t\tc0.185-0.521,0.753-0.795,1.276-0.61c0.521,0.184,0.794,0.755,0.61,1.276l-6,17C25.298,31.744,24.912,32,24.5,32z\"/>";
	private const string COMIC = "<linearGradient id=\"IconifyId17ecdb2904d178eab20604\" gradientUnits=\"userSpaceOnUse\" x1=\"79.567\" y1=\"78.962\" x2=\"51.086\" y2=\"40.814\" gradientTransform=\"matrix(0.251989, 0, 0, -0.251989, 9.762631, 36.827506)\">      <stop offset=\"0\" stop-color=\"#a52714\"/>      <stop offset=\"0.529\" stop-color=\"#d23f31\"/>      <stop offset=\"1\" stop-color=\"#ed4132\"/>    </linearGradient>    <radialGradient id=\"IconifyId17ecdb2904d178eab20605\" cx=\"74.878\" cy=\"64.629\" r=\"18.579\" gradientTransform=\"matrix(0.235912, 0.088574, 0.17163, -0.457158, -0.126667, 43.455565)\" gradientUnits=\"userSpaceOnUse\">      <stop offset=\"0\" stop-color=\"#212121\"/>      <stop offset=\"0.999\" stop-color=\"#212121\" stop-opacity=\"0\"/>    </radialGradient>    <linearGradient id=\"IconifyId17ecdb2904d178eab20606\" gradientUnits=\"userSpaceOnUse\" x1=\"44.406\" y1=\"72.898\" x2=\"59.099\" y2=\"47.53\" gradientTransform=\"matrix(0.251989, 0, 0, -0.251989, 9.762631, 36.827506)\">      <stop offset=\"0\" stop-color=\"#212121\"/>      <stop offset=\"0.999\" stop-color=\"#212121\" stop-opacity=\"0\"/>    </linearGradient>    <linearGradient id=\"IconifyId17ecdb2904d178eab20607\" gradientUnits=\"userSpaceOnUse\" x1=\"76.007\" y1=\"86.58\" x2=\"75.835\" y2=\"66.902\" gradientTransform=\"matrix(0.251989, 0, 0, -0.251989, 9.762631, 36.827506)\">      <stop offset=\"0\" stop-color=\"#42a5f5\"/>      <stop offset=\"1\" stop-color=\"#1e88e5\"/>    </linearGradient>    <linearGradient id=\"IconifyId17ecdb2904d178eab20608\" gradientUnits=\"userSpaceOnUse\" x1=\"50.366\" y1=\"75.374\" x2=\"43.808\" y2=\"89.607\" gradientTransform=\"matrix(0.251989, 0, 0, -0.251989, 9.762631, 36.827506)\">      <stop offset=\"0\" stop-color=\"#a52714\"/>      <stop offset=\"0.529\" stop-color=\"#d23f31\"/>      <stop offset=\"1\" stop-color=\"#db4437\"/>    </linearGradient>    <linearGradient id=\"IconifyId17ecdb2904d178eab20609\" gradientUnits=\"userSpaceOnUse\" x1=\"55.129\" y1=\"43.972\" x2=\"50.129\" y2=\"36.222\" gradientTransform=\"matrix(0.251989, 0, 0, -0.251989, 9.762631, 36.827506)\">      <stop offset=\"0.001\" stop-color=\"#851f10\"/>      <stop offset=\"0.841\" stop-color=\"#a52714\"/>    </linearGradient>    <path d=\"M 31.946 12.652 C 32.097 12.559 32.294 12.597 32.394 12.743 C 32.924 13.509 34.332 16.062 33.798 20.925 C 33.45 24.082 31.661 28.641 31.054 30.117 C 30.967 30.333 30.69 30.394 30.52 30.236 C 29.91 29.679 28.421 28.739 25.709 29.369 C 24.85 29.568 22.169 32.254 20.014 29.835 C 18.684 28.341 19.654 27.462 19.664 25.312 C 19.677 22.545 16.892 21.696 15.983 21.242 C 15.75 21.127 15.733 20.807 15.95 20.665 C 17.006 19.98 18.487 19.657 20.349 18.176 C 22.714 16.299 22.733 14.539 23.217 13.93 C 23.461 13.622 25.387 12.758 25.387 12.758 C 25.387 12.758 26.732 13.116 26.798 13.141 L 29.199 14.202 C 29.295 14.237 29.403 14.227 29.492 14.172 L 31.946 12.652 Z\" fill=\"url(#IconifyId17ecdb2904d178eab20604)\" style=\"opacity: 1;\"/>    <path d=\"M 32.553 23.143 C 33.917 18.922 32.928 15.455 32.928 15.455 C 32.928 15.455 28.927 16.868 28.917 17.032 C 28.91 17.195 26.316 23.359 26.316 23.359 C 26.316 23.359 24.429 28.465 22.035 30.838 C 23.751 30.652 25.139 29.5 25.709 29.369 C 27.496 28.953 28.749 29.22 29.572 29.611 C 30.024 28.893 31.596 26.111 32.553 23.143 Z\" fill=\"url(#IconifyId17ecdb2904d178eab20605)\" style=\"opacity: 1;\"/>    <path d=\"M 26.891 15.565 L 25.614 14.986 C 25.614 14.986 23.588 19.61 22.562 21.204 C 21.534 22.799 19.664 25.312 19.664 25.312 C 19.666 27.462 18.681 28.341 20.014 29.835 C 20.103 29.936 20.196 30.024 20.287 30.107 C 21.141 29.082 22.551 27.078 23.61 25.576 C 26.471 21.517 26.891 15.565 26.891 15.565 Z\" fill=\"url(#IconifyId17ecdb2904d178eab20606)\" style=\"opacity: 1;\"/>    <path d=\"M 35.854 10.712 C 35.6 10.019 34.355 8.031 34.355 8.031 L 32.909 8.726 L 33.483 11.211 L 30.895 13.365 C 29.825 13.118 28.719 13.074 27.632 13.234 C 26.891 13.179 24.784 13.204 23.988 15.142 L 22.834 17.888 L 21.899 18.213 C 21.639 18.756 22.491 19.244 23.033 19.501 C 23.575 19.759 24.041 19.559 24.485 18.987 C 24.749 18.645 25.364 17.684 25.813 16.971 C 25.913 17.735 25.984 18.667 25.989 19.801 L 28.088 20.811 L 30.643 20.92 C 31.575 18.811 32.057 17.205 32.302 16.069 C 33.145 15.116 35.333 12.642 35.615 12.249 C 35.963 11.766 36.108 11.405 35.854 10.712 Z\" fill=\"url(#IconifyId17ecdb2904d178eab20607)\" style=\"opacity: 1;\"/>    <path d=\"M 35.854 10.712 C 35.6 10.019 34.355 8.031 34.355 8.031 L 32.909 8.726 L 33.483 11.211 L 30.895 13.365 C 29.825 13.118 28.719 13.074 27.632 13.234 C 26.891 13.179 24.784 13.204 23.988 15.142 L 22.834 17.888 L 21.899 18.213 C 21.639 18.756 22.491 19.244 23.033 19.501 C 23.575 19.759 24.041 19.559 24.485 18.987 C 24.749 18.645 25.364 17.684 25.813 16.971 C 25.913 17.735 25.984 18.667 25.989 19.801 L 28.088 20.811 L 30.643 20.92 C 31.575 18.811 32.057 17.205 32.302 16.069 C 33.145 15.116 35.333 12.642 35.615 12.249 C 35.963 11.766 36.108 11.405 35.854 10.712 Z\" fill=\"none\" opacity=\".29\" style=\"opacity: 0.5;\"/>    <path d=\"M 20.249 14.305 C 20.397 14.033 20.864 13.847 21.199 13.743 C 21.403 13.68 21.62 13.786 21.7 13.98 L 22.197 15.169 C 22.257 15.315 22.229 15.485 22.124 15.606 L 21.657 16.13 C 21.829 16.361 22.043 16.548 22.29 16.696 C 22.71 16.951 23.132 16.941 23.174 16.976 C 23.709 17.407 24.106 17.911 23.794 18.737 C 23.484 19.559 22.416 19.307 22.416 19.307 C 22.416 19.307 21.32 18.536 20.614 16.863 C 20.385 16.326 19.939 14.87 20.249 14.305 Z\" fill=\"url(#IconifyId17ecdb2904d178eab20608)\" style=\"opacity: 1;\"/>    <path d=\"M 20.249 14.305 C 20.397 14.033 20.864 13.847 21.199 13.743 C 21.403 13.68 21.62 13.786 21.7 13.98 L 22.197 15.169 C 22.257 15.315 22.229 15.485 22.124 15.606 L 21.657 16.13 C 21.829 16.361 22.043 16.548 22.29 16.696 C 22.71 16.951 23.132 16.941 23.174 16.976 C 23.709 17.407 24.106 17.911 23.794 18.737 C 23.484 19.559 22.416 19.307 22.416 19.307 C 22.416 19.307 21.32 18.536 20.614 16.863 C 20.385 16.326 19.939 14.87 20.249 14.305 Z\" fill=\"none\" opacity=\".29\" style=\"opacity: 0.5;\"/>    <path d=\"M 23.038 25.451 C 23.038 25.451 21.642 26.749 21.539 27.33 C 21.367 28.295 22.184 29.188 22.608 28.842 C 23.527 28.096 23.777 27.171 24.233 26.501 C 24.686 25.831 23.643 24.838 23.038 25.451 Z\" fill=\"url(#IconifyId17ecdb2904d178eab20609)\" style=\"opacity: 1;\"/>    <g transform=\"matrix(0.251989, 0, 0, 0.251989, 9.762633, 4.572948)\" style=\"opacity: 1;\">      <linearGradient id=\"IconifyId17ecdb2904d178eab20610\" gradientUnits=\"userSpaceOnUse\" x1=\"69.79\" y1=\"55.927\" x2=\"60.641\" y2=\"35.386\" gradientTransform=\"matrix(1 0 0 -1 0 128)\">        <stop offset=\"0\" stop-color=\"#42a5f5\"/>        <stop offset=\"1\" stop-color=\"#1e88e5\"/>      </linearGradient>      <path d=\"M71.4 66.77l-.25.68c-.45-1.66-1.51-3.18-2.95-3.95c0 0-5.78-1.91-7.23.78c0 0-6.93 10.92-8.03 13.96c-1.11 3.04-1.07 5.52 1.53 7.13c2.1 1.29 4.92.67 7.5-2.26c1.16-1.32 3.84-5.35 5.88-8.48c.32-.5 1.09-.11.88.45l-12.12 31.91l5.41 3.44s16.1-25.73 18.47-30.65c2.37-4.92 3.21-12.14 3.21-12.14l-12.3-.87z\" fill=\"url(#IconifyId17ecdb2904d178eab20610)\"/>    </g>    <g transform=\"matrix(0.251989, 0, 0, 0.251989, 9.762633, 4.572948)\" style=\"opacity: 1;\">      <path d=\"M61.24 63.89l2-3.26c.28-.63 1-.89 1.58-.56c6.62 3.78 10.14 4.79 17.67 4.34c.69-.04 1.26.58 1.21 1.33l-.26 3.51s-2.76 1.56-11.19-.23c-5.98-1.27-11.01-5.13-11.01-5.13z\" fill=\"#212121\" opacity=\".4\"/>      <linearGradient id=\"IconifyId17ecdb2904d178eab20611\" gradientUnits=\"userSpaceOnUse\" x1=\"62.701\" y1=\"65.551\" x2=\"84.321\" y2=\"60.826\" gradientTransform=\"matrix(1 0 0 -1 0 128)\">        <stop offset=\"0\" stop-color=\"#d32f2f\"/>        <stop offset=\"0.23\" stop-color=\"#f44336\"/>        <stop offset=\"0.742\" stop-color=\"#f44336\"/>        <stop offset=\"1\" stop-color=\"#d32f2f\"/>      </linearGradient>      <path d=\"M83.08 68.94c-8.48.45-13.79-1.08-21.19-5.01c-.58-.31-.81-1.02-.53-1.62l1.19-2.52c.3-.63 1.07-.89 1.69-.56c7.09 3.78 10.86 4.78 18.94 4.34c.74-.04 1.36.59 1.29 1.33l-.23 2.9c-.05.62-.55 1.11-1.16 1.14z\" fill=\"url(#IconifyId17ecdb2904d178eab20611)\"/>      <g>        <path d=\"M75.57 66.99l-2.31 1.53c-.46.3-1.07.18-1.37-.28l-1.53-2.31a.985.985 0 0 1 .28-1.37l2.31-1.53a.985.985 0 0 1 1.37.28l1.53 2.31c.3.45.18 1.06-.28 1.37z\" fill=\"#fdd835\"/>      </g>    </g>    <g transform=\"matrix(0.251989, 0, 0, 0.251989, 9.762633, 4.572948)\" style=\"opacity: 1;\">      <path d=\"M70.48 45.02c1.28-.56 3.66-1.34 7.09-1.47c.22-.01.43-.01.63-.01c3.09 0 4.98.84 5.95 1.43l-6.94 7.46l-6.73-7.41z\" fill=\"#e53935\"/>      <path d=\"M78.21 44.58c1.89 0 3.28.34 4.25.71l-5.23 5.62l-4.95-5.45c1.27-.4 3.06-.79 5.33-.87c.21-.01.4-.01.6-.01m0-2.07c-.22 0-.44 0-.67.01c-5.77.21-8.73 2.2-8.73 2.2l8.4 9.25l8.48-9.12c-.01 0-2.19-2.34-7.48-2.34z\" fill=\"#fdd835\"/>      <linearGradient id=\"IconifyId17ecdb2904d178eab20612\" gradientUnits=\"userSpaceOnUse\" x1=\"77.247\" y1=\"83.257\" x2=\"77.247\" y2=\"73.402\" gradientTransform=\"matrix(1 0 0 -1 0 128)\">        <stop offset=\"0\" stop-color=\"#0d47a1\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#0d47a1\"/>      </linearGradient>      <path d=\"M68.81 44.72s2.96-1.99 8.73-2.2c5.77-.21 8.15 2.33 8.15 2.33l-8.48 9.12l-8.4-9.25z\" opacity=\".42\" fill=\"url(#IconifyId17ecdb2904d178eab20612)\"/>    </g>    <g transform=\"matrix(0.251989, 0, 0, 0.251989, 9.762633, 4.572948)\" style=\"opacity: 1;\">      <linearGradient id=\"IconifyId17ecdb2904d178eab20613\" gradientUnits=\"userSpaceOnUse\" x1=\"66.098\" y1=\"27.623\" x2=\"43.784\" y2=\"5.309\" gradientTransform=\"matrix(1 0 0 -1 0 128)\">        <stop offset=\"0\" stop-color=\"#a52714\"/>        <stop offset=\"0.529\" stop-color=\"#d23f31\"/>        <stop offset=\"1\" stop-color=\"#ed4132\"/>      </linearGradient>      <path d=\"M57.86 97.6s-1.61 9.66-5.58 15.98c-1.94 2.57-5.1 4.68-6.74 9.09c-.75 2.03 3.63 3.96 7.04 2.09c2.05-1.13 5.15-8.03 5.15-8.03c2.74-4.98 10.9-12.92 10.9-12.92c-1.57-7.29-10.77-6.21-10.77-6.21z\" fill=\"url(#IconifyId17ecdb2904d178eab20613)\"/>    </g>    <g transform=\"matrix(0.251989, 0, 0, 0.251989, 9.762633, 4.572948)\" style=\"opacity: 1;\">      <path d=\"M53.4 37.12s5.69-7.18 15.85-5.13c7.55 1.52 5.96 6.37 5.96 6.37s-3.81.03-7.24-.76c-3.47-.8-6.56-2.41-8.4-2.22l-6.17 1.74z\" fill=\"#ed4132\"/>      <path d=\"M75.21 38.37s.47-4.91 3.79-6.1s10.76-2.29 10.76-2.29s-2.33 4.56-4.13 6c-3.8 3.03-10.42 2.39-10.42 2.39z\" fill=\"#ed4132\"/>      <path d=\"M57.13 40.53s3.26-2.94 8.22-1.75c7.14 1.71 9.86-.41 9.86-.41s5.25 1.89 10.22-.5c3.41-1.64 4.34-7.89 4.34-7.89L88.6 31.3s-4.38 4.77-8.74 6.06c-3.19.95-4.64 1.01-4.64 1.01s-11.76-4.03-14.44-2.91s-3.65 5.07-3.65 5.07z\" opacity=\".4\" fill=\"#212121\"/>    </g>    <g transform=\"matrix(0.251989, 0, 0, 0.251989, 9.762633, 4.572948)\" style=\"opacity: 1;\">      <linearGradient id=\"IconifyId17ecdb2904d178eab20614\" gradientUnits=\"userSpaceOnUse\" x1=\"98.368\" y1=\"104.084\" x2=\"84.559\" y2=\"124.97\" gradientTransform=\"matrix(1 0 0 -1 0 128)\">        <stop offset=\"0\" stop-color=\"#a52714\"/>        <stop offset=\"0.529\" stop-color=\"#d23f31\"/>        <stop offset=\"1\" stop-color=\"#db4437\"/>      </linearGradient>      <path d=\"M92.68 24.92c-.14-.93-1.91-15.14-5.23-15.88c0 0-1.76.02-3.56.18c-1.42.05-2.36-.33-2.63-1.22c-.33-.68.01-2.2.58-3.9c.57-1.71 1.59-1.92 2.4-1.94c1.57-.04 4.25 1.55 7.93 4.73c7.08 6.12 10.95 15.35 10.95 15.35c-.13 0-4.69 6.23-10.44 2.68z\" fill=\"url(#IconifyId17ecdb2904d178eab20614)\"/>      <g opacity=\".29\">        <path d=\"M92.68 24.92c-.14-.93-1.91-15.14-5.23-15.88c0 0-1.76.02-3.56.18c-1.42.05-2.36-.33-2.63-1.22c-.33-.68.01-2.2.58-3.9c.57-1.71 1.59-1.92 2.4-1.94c1.57-.04 4.25 1.55 7.93 4.73c7.08 6.12 10.95 15.35 10.95 15.35c-.13 0-4.69 6.23-10.44 2.68z\" fill=\"none\"/>      </g>    </g>    <g transform=\"matrix(0.251989, 0, 0, 0.251989, 9.762633, 4.572948)\" style=\"opacity: 1;\">      <path d=\"M84.87 29.2c-.38-.63.05-1.31.78-2.15c1.17-1.34 2.38-4.38 1.03-7.37c.01-.02-.26-.52-.26-.54h-.51c-.16-.02-5.76.19-11.36.42c-5.6.22-11.19.46-11.35.49c0 0-.73.58-.72.6c-1.12 3.09.34 6.02 1.61 7.26c.8.78 1.28 1.42.95 2.08c-.32.64-1.34.77-1.34.77s.26.63.84.95c.54.3 1.19.35 1.64.34c0 0 1.84 2.35 6.41 2.17l2.54-.1l2.54-.1c4.57-.18 6.22-2.67 6.22-2.67c.45-.03 1.09-.13 1.6-.47c.56-.37.77-1.02.77-1.02s-1.02-.04-1.39-.66z\" fill=\"#543930\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20615\" cx=\"99.569\" cy=\"93.17\" r=\"6.657\" gradientTransform=\"matrix(.9992 -.0399 .0196 .4908 -22.194 -11.663)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.728\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\"/>      </radialGradient>      <path d=\"M75.13 34.13l-.11-2.68l8.09-.97l.78.88s-1.65 2.49-6.22 2.67l-2.54.1z\" fill=\"url(#IconifyId17ecdb2904d178eab20615)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20616\" cx=\"93.012\" cy=\"94.16\" r=\"1.968\" gradientTransform=\"matrix(-.8881 .4597 -.341 -.6588 200.347 48.116)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.663\" stop-color=\"#6d4c41\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>      </radialGradient>      <path d=\"M83.82 30.19c-1.29-1.8 1.38-2.59 1.38-2.59c-.45.6-.64 1.12-.34 1.61c.37.61 1.4.66 1.4.66s-1.34 1.29-2.44.32z\" fill=\"url(#IconifyId17ecdb2904d178eab20616)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20617\" cx=\"98.111\" cy=\"99.726\" r=\"8.642\" gradientTransform=\"matrix(-.1144 -.9934 .828 -.0953 8.966 130.45)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.725\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\"/>      </radialGradient>      <path d=\"M86.68 19.68c1.32 2.89.18 5.97-.96 7.3c-.16.18-.82.89-.96 1.44c0 0-2.86-3.67-3.76-5.86c-.18-.44-.35-.9-.39-1.37c-.03-.36.01-.78.2-1.1c.24-.38 5.71-.71 5.71-.71c.01 0 .16.3.16.3z\" fill=\"url(#IconifyId17ecdb2904d178eab20617)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20618\" cx=\"68.909\" cy=\"99.726\" r=\"8.642\" gradientTransform=\"matrix(.0347 -.9994 -.8329 -.029 149.791 95.688)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.725\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\"/>      </radialGradient>      <path d=\"M62.48 20.65c-1.09 2.99.3 5.97 1.54 7.2c.17.17.89.82 1.07 1.36c0 0 2.56-3.89 3.28-6.14c.14-.45.28-.92.28-1.4c0-.36-.07-.78-.29-1.08c-.27-.36-.56-.25-.99-.23c-.82.03-4.41-.03-4.69-.02c0-.01-.2.31-.2.31z\" fill=\"url(#IconifyId17ecdb2904d178eab20618)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20619\" cx=\"70.367\" cy=\"93.17\" r=\"6.657\" gradientTransform=\"matrix(-.9992 .0399 .0196 .4908 139.328 -18.118)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.728\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\"/>      </radialGradient>      <path d=\"M75.14 34.13l-.11-2.68l-8.14-.32l-.7.94s1.84 2.35 6.41 2.17l2.54-.11z\" fill=\"url(#IconifyId17ecdb2904d178eab20619)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20620\" cx=\"63.81\" cy=\"94.16\" r=\"1.968\" gradientTransform=\"matrix(.922 .3873 .2873 -.6839 -21.629 69.373)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.663\" stop-color=\"#6d4c41\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>      </radialGradient>      <path d=\"M66.16 30.89c1.14-1.9-1.58-2.48-1.58-2.48c.49.56.72 1.06.47 1.58c-.32.64-1.34.77-1.34.77s1.44 1.19 2.45.13z\" fill=\"url(#IconifyId17ecdb2904d178eab20620)\"/>    </g>    <g transform=\"matrix(0.251989, 0, 0, 0.251989, 9.762633, 4.572948)\" style=\"opacity: 1;\">      <path d=\"M75.01 31.28l-3.22.09l.09 2.5c0 3.16 3.36 4.5 3.36 4.5s3.07-1.65 3.07-4.55l-.09-2.62l-3.21.08z\" fill=\"#e59600\"/>      <path d=\"M82.43 21.49l-2.08.06l-11.26.31l-2.08.06c-1.66.05-2.98 1.53-2.93 3.3c.05 1.77 1.45 3.18 3.11 3.14l2.08-.06l11.26-.31l2.08-.06c1.66-.05 2.98-1.53 2.93-3.3s-1.45-3.18-3.11-3.14z\" fill=\"#e59600\"/>      <path d=\"M74.38 10.32c-4.91.13-9.31 5.51-9.1 13.06c.21 7.51 4.99 11.1 9.76 10.97s9.35-3.97 9.15-11.49c-.22-7.55-4.91-12.67-9.81-12.54z\" fill=\"#ffca28\"/>      <g fill=\"#404040\">        <ellipse transform=\"rotate(-1.564 70.157 24.467)\" cx=\"70.15\" cy=\"24.47\" rx=\"1.39\" ry=\"1.44\"/>        <ellipse transform=\"rotate(-1.564 79.428 24.216)\" cx=\"79.42\" cy=\"24.21\" rx=\"1.39\" ry=\"1.44\"/>      </g>      <path d=\"M77.24 29.42c-.88.56-3.83.64-4.74.13c-.52-.29-1.04.19-.81.65c.22.45 1.86 1.48 3.25 1.44c1.39-.04 2.95-1.15 3.14-1.62c.2-.46-.33-.92-.84-.6z\" fill=\"#795548\"/>      <path d=\"M75.76 26.87a.292.292 0 0 0-.09-.02l-2 .06c-.03 0-.06.01-.09.03c-.18.08-.27.27-.18.47s.5.75 1.3.72c.8-.02 1.18-.59 1.26-.79c.08-.22-.02-.41-.2-.47z\" fill=\"#e59600\"/>      <g fill=\"#6d4c41\">        <path d=\"M72.21 21.79c-.27-.34-.9-.84-2.09-.81s-1.79.56-2.04.92c-.11.16-.08.34.01.44c.08.1.3.19.55.1s.72-.35 1.52-.38c.8-.02 1.29.22 1.54.3c.25.08.47-.02.54-.13a.38.38 0 0 0-.03-.44z\"/>        <path d=\"M81.49 21.54c-.27-.34-.9-.84-2.09-.81c-1.19.03-1.79.56-2.04.92c-.11.16-.08.34.01.44c.08.1.3.19.55.1s.72-.35 1.52-.38c.8-.02 1.29.22 1.54.3c.25.08.47-.02.54-.13c.06-.11.09-.29-.03-.44z\"/>      </g>    </g>    <g transform=\"matrix(0.251989, 0, 0, 0.251989, 9.762633, 4.572948)\" style=\"opacity: 1;\">      <path d=\"M85.71 13.6c-.74-1.02-2.35-2.37-3.74-2.4c-.28-1.33-1.76-2.41-3.16-2.8c-3.81-1.04-6.2.39-7.48 1.16c-.27.16-1.99 1.21-3.25.56c-.79-.41-.82-1.6-.82-1.6s-2.39 1.02-1.45 3.56c-.83.07-1.91.46-2.44 1.65c-.63 1.42-.34 2.57-.12 3.12c-.69.64-1.54 1.97-.86 3.62c.52 1.25 2.4 1.75 2.4 1.75c-.04 2.28.44 3.66.69 4.22c.04.1.18.08.21-.02c.24-1.14 1.04-5.1.92-5.79c0 0 3.2-.77 6.18-3.15c.61-.49 1.27-.91 1.98-1.22c3.8-1.69 4.71.9 4.71.9s2.65-.62 3.61 3.06c.36 1.38.65 3.6.89 5.15c.02.11.17.13.21.02c.25-.63.74-1.88.82-3.14c.03-.44 1.19-1.07 1.63-2.98c.54-2.54-.39-4.93-.93-5.67z\" fill=\"#543930\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20621\" cx=\"79.629\" cy=\"104.284\" r=\"10.13\" gradientTransform=\"matrix(.3454 .9385 .6963 -.2562 -20.46 -29.068)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.699\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\"/>      </radialGradient>      <path d=\"M84.96 22.27c.03-.44 1.19-1.07 1.63-2.98c.05-.2.08-.41.12-.62c.32-2.32-.51-4.4-1-5.07c-.68-.94-2.11-2.16-3.42-2.37c-.11-.01-.22-.02-.33-.02c0 0 .12.61-.11 1.1c-.29.64-.94.82-.94.82c3.54 3.27 3.41 6.12 4.05 9.14z\" fill=\"url(#IconifyId17ecdb2904d178eab20621)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20622\" cx=\"69.753\" cy=\"115.329\" r=\"2.656\" gradientTransform=\"matrix(.8995 .437 .5182 -1.0665 -53.159 100.819)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.58\" stop-color=\"#6d4c41\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>      </radialGradient>      <path d=\"M72.13 9.1c-.31.16-.57.32-.81.46c-.27.16-1.99 1.21-3.25.56c-.78-.4-.82-1.55-.82-1.6c-.33.46-1.26 3.69 1.84 3.78c1.34.04 2.11-1.16 2.56-2.16c.15-.37.41-.9.48-1.04z\" fill=\"url(#IconifyId17ecdb2904d178eab20622)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20623\" cx=\"151.972\" cy=\"68.119\" r=\"8.165\" gradientTransform=\"matrix(-.9528 -.3566 -.1969 .5367 233.682 30.527)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.699\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\"/>      </radialGradient>      <path d=\"M78.41 8.3c2.1.48 3.16 1.5 3.55 2.9c.12.41.39 4.27-7.17.17c-2.81-1.53-2.1-2.58-1.79-2.71c1.25-.5 3.06-.9 5.41-.36z\" fill=\"url(#IconifyId17ecdb2904d178eab20623)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20624\" cx=\"68.687\" cy=\"112.906\" r=\"2.438\" gradientTransform=\"matrix(.9992 -.0399 -.0489 -1.2223 5.266 151.515)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.702\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\"/>      </radialGradient>      <path d=\"M67.24 8.52s-.01 0-.02.01c-.26.12-2.3 1.17-1.43 3.54l2.22.27c-2.03-1.9-.76-3.82-.77-3.82c.01 0 0 0 0 0z\" fill=\"url(#IconifyId17ecdb2904d178eab20624)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20625\" cx=\"68.351\" cy=\"108.603\" r=\"4.572\" gradientTransform=\"matrix(-.9753 -.2211 -.2069 .9127 157.344 -68.934)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.66\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\"/>      </radialGradient>      <path d=\"M67.17 12.24l-1.37-.16c-.05 0-.24.03-.33.04c-.76.14-1.66.57-2.1 1.61c-.48 1.12-.43 2.06-.26 2.68c.05.21.15.44.15.44s.65-.67 2.26-.77l1.65-3.84z\" fill=\"url(#IconifyId17ecdb2904d178eab20625)\"/>      <radialGradient id=\"IconifyId17ecdb2904d178eab20626\" cx=\"67.266\" cy=\"104.189\" r=\"4.8\" gradientTransform=\"matrix(.9953 .0966 .1358 -1.3986 -13.795 158.75)\" gradientUnits=\"userSpaceOnUse\">        <stop offset=\"0.598\" stop-color=\"#6d4c41\" stop-opacity=\"0\"/>        <stop offset=\"1\" stop-color=\"#6d4c41\"/>      </radialGradient>      <path d=\"M63.17 16.91c-.65.62-1.5 1.99-.77 3.62c.55 1.23 2.37 1.7 2.37 1.7s.36.1.55.09l.17-6.24c-.86.03-1.68.33-2.2.72c.01 0-.12.1-.12.11z\" fill=\"url(#IconifyId17ecdb2904d178eab20626)\"/>    </g>";
	private const string IMAGE = "<circle style=\"fill:#F3D55B;\" cx=\"18.931\" cy=\"14.431\" r=\"4.569\"/>\t<polygon style=\"fill:{COLOR};\" points=\"6.5,39 17.5,39 49.5,39 49.5,28 39.5,18.5 29,30 23.517,24.517 \t\"/>\t";
	private const string TEXT = "<path style=\"fill:#C8BDB8;\" d=\"M18.5,13h-6c-0.553,0-1-0.448-1-1s0.447-1,1-1h6c0.553,0,1,0.448,1,1S19.053,13,18.5,13z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M21.5,18h-9c-0.553,0-1-0.448-1-1s0.447-1,1-1h9c0.553,0,1,0.448,1,1S22.053,18,21.5,18z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M25.5,18c-0.26,0-0.521-0.11-0.71-0.29c-0.181-0.19-0.29-0.44-0.29-0.71s0.109-0.52,0.3-0.71\t\tc0.36-0.37,1.04-0.37,1.41,0c0.18,0.19,0.29,0.45,0.29,0.71c0,0.26-0.11,0.52-0.29,0.71C26.02,17.89,25.76,18,25.5,18z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M37.5,18h-8c-0.553,0-1-0.448-1-1s0.447-1,1-1h8c0.553,0,1,0.448,1,1S38.053,18,37.5,18z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M12.5,33c-0.26,0-0.521-0.11-0.71-0.29c-0.181-0.19-0.29-0.45-0.29-0.71\t\tc0-0.26,0.109-0.52,0.29-0.71c0.37-0.37,1.05-0.37,1.42,0.01c0.18,0.18,0.29,0.44,0.29,0.7c0,0.26-0.11,0.52-0.29,0.71\t\tC13.02,32.89,12.76,33,12.5,33z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M24.5,33h-8c-0.553,0-1-0.448-1-1s0.447-1,1-1h8c0.553,0,1,0.448,1,1S25.053,33,24.5,33z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M43.5,18h-2c-0.553,0-1-0.448-1-1s0.447-1,1-1h2c0.553,0,1,0.448,1,1S44.053,18,43.5,18z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M34.5,23h-22c-0.553,0-1-0.448-1-1s0.447-1,1-1h22c0.553,0,1,0.448,1,1S35.053,23,34.5,23z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M43.5,23h-6c-0.553,0-1-0.448-1-1s0.447-1,1-1h6c0.553,0,1,0.448,1,1S44.053,23,43.5,23z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M16.5,28h-4c-0.553,0-1-0.448-1-1s0.447-1,1-1h4c0.553,0,1,0.448,1,1S17.053,28,16.5,28z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M30.5,28h-10c-0.553,0-1-0.448-1-1s0.447-1,1-1h10c0.553,0,1,0.448,1,1S31.053,28,30.5,28z\"/>\t<path style=\"fill:#C8BDB8;\" d=\"M43.5,28h-9c-0.553,0-1-0.448-1-1s0.447-1,1-1h9c0.553,0,1,0.448,1,1S44.053,28,43.5,28z\"/>";
	private const string VIDEO = @"<polygon style=""fill:#fff;"" points=""23.5,28 23.5,20.954 23.5,14 34.5,21 ""/>";
	private const string PDF = "<path style=\"fill:#fff;\" d=\"M19.514,33.324L19.514,33.324c-0.348,0-0.682-0.113-0.967-0.326\n\t\tc-1.041-0.781-1.181-1.65-1.115-2.242c0.182-1.628,2.195-3.332,5.985-5.068c1.504-3.296,2.935-7.357,3.788-10.75\n\t\tc-0.998-2.172-1.968-4.99-1.261-6.643c0.248-0.579,0.557-1.023,1.134-1.215c0.228-0.076,0.804-0.172,1.016-0.172\n\t\tc0.504,0,0.947,0.649,1.261,1.049c0.295,0.376,0.964,1.173-0.373,6.802c1.348,2.784,3.258,5.62,5.088,7.562\n\t\tc1.311-0.237,2.439-0.358,3.358-0.358c1.566,0,2.515,0.365,2.902,1.117c0.32,0.622,0.189,1.349-0.39,2.16\n\t\tc-0.557,0.779-1.325,1.191-2.22,1.191c-1.216,0-2.632-0.768-4.211-2.285c-2.837,0.593-6.15,1.651-8.828,2.822\n\t\tc-0.836,1.774-1.637,3.203-2.383,4.251C21.273,32.654,20.389,33.324,19.514,33.324z M22.176,28.198\n\t\tc-2.137,1.201-3.008,2.188-3.071,2.744c-0.01,0.092-0.037,0.334,0.431,0.692C19.685,31.587,20.555,31.19,22.176,28.198z\n\t\t M35.813,23.756c0.815,0.627,1.014,0.944,1.547,0.944c0.234,0,0.901-0.01,1.21-0.441c0.149-0.209,0.207-0.343,0.23-0.415\n\t\tc-0.123-0.065-0.286-0.197-1.175-0.197C37.12,23.648,36.485,23.67,35.813,23.756z M28.343,17.174\n\t\tc-0.715,2.474-1.659,5.145-2.674,7.564c2.09-0.811,4.362-1.519,6.496-2.02C30.815,21.15,29.466,19.192,28.343,17.174z\n\t\t M27.736,8.712c-0.098,0.033-1.33,1.757,0.096,3.216C28.781,9.813,27.779,8.698,27.736,8.712z\"/>\n\t";
	private const string DISC = "\n\t<circle style=\"fill:#C8BDB8;\" cx=\"27.5\" cy=\"21\" r=\"12\"/>\n\t<circle style=\"fill:#E9E9E0;\" cx=\"27.5\" cy=\"21\" r=\"3\"/>\n\t<path style=\"fill:#D3CCC9;\" d=\"M25.379,18.879c0.132-0.132,0.276-0.245,0.425-0.347l-2.361-8.813\n\t\tc-1.615,0.579-3.134,1.503-4.427,2.796c-1.294,1.293-2.217,2.812-2.796,4.427l8.813,2.361\n\t\tC25.134,19.155,25.247,19.011,25.379,18.879z\"/>\n\t<path style=\"fill:#D3CCC9;\" d=\"M30.071,23.486l2.273,8.483c1.32-0.582,2.56-1.402,3.641-2.484c1.253-1.253,2.16-2.717,2.743-4.275\n\t\tl-8.188-2.194C30.255,22.939,29.994,23.2,30.071,23.486z\"/>";
	private const string URL =  "<path fill=\"#C8BDB8\" fill-rule=\"evenodd\" class=\"c\" d=\"m28 36c-5.7 0-10.8-3.4-12.9-8.6-2.2-5.3-1-11.3 3-15.3 4-4 10-5.2 15.3-3 5.2 2.1 8.6 7.2 8.6 12.9 0 1.8-0.4 3.7-1.1 5.4-0.7 1.7-1.7 3.2-3 4.5-1.3 1.3-2.8 2.3-4.5 3-1.7 0.7-3.6 1.1-5.4 1.1zm0-26.8c-5.2 0-9.9 3.1-11.8 7.9-2 4.8-0.9 10.3 2.7 14 3.7 3.6 9.2 4.7 14 2.7 4.8-1.9 7.9-6.6 7.9-11.8 0-1.7-0.3-3.4-1-4.9-0.6-1.6-1.5-3-2.7-4.2-1.2-1.2-2.6-2.1-4.2-2.7-1.5-0.7-3.2-1-4.9-1z\" />\n        <path fill=\"#C8BDB8\" class=\"c\" d=\"m14.8 21.4h26.4v1.2h-26.4z\" />\n        <path fill=\"#C8BDB8\" class=\"c\"\n              d=\"m28.3 16.5q-1.5 0-3-0.1-1.5-0.1-3-0.3-1.5-0.2-3-0.5-1.4-0.3-2.9-0.7l0.4-1.1c0.1 0 11.2 3.3 22.4 0l0.3 1.1q-1.3 0.4-2.7 0.7-1.4 0.3-2.8 0.5-1.4 0.2-2.8 0.3-1.5 0.1-2.9 0.1z\" /><path fill=\"#C8BDB8\" class=\"c\" d=\"m16.8 29.9l-0.4-1.1c11.5-3.4 22.7-0.2 23.1 0l-0.3 1.1c-0.1 0-11.3-3.3-22.4 0z\" />\n        <path fill=\"#C8BDB8\" class=\"c\"\n              d=\"m23.5 34.8q-1.3-3.1-2-6.3-0.7-3.2-0.8-6.5-0.1-3.3 0.4-6.6 0.5-3.2 1.6-6.4l1.1 0.5q-1.1 3-1.5 6.2-0.5 3.1-0.4 6.3 0.1 3.2 0.7 6.3 0.7 3.1 2 6z\" /><path fill=\"#C8BDB8\" class=\"c\" d=\"m32.4 34.8l-1-0.5q1.2-2.9 1.9-6 0.7-3.1 0.8-6.3 0-3.2-0.4-6.3-0.5-3.2-1.6-6.2l1.1-0.4q1.1 3.1 1.6 6.3 0.5 3.3 0.4 6.6-0.1 3.3-0.8 6.5-0.7 3.2-2 6.3z\" /><path fill=\"#C8BDB8\" class=\"d\" d=\"m48 56h-40c-0.8 0-1.5-0.7-1.5-1.5v-15.5h43v15.5c0 0.8-0.7 1.5-1.5 1.5z\" />";

	private static List<string> VideoExtensions = new() { "mkv", "mov", "mp4", "mpeg", "mpg","avi", "ts", "webm", "wmv" };
	private static List<string> ImageExtensions = new() { "bmp", "gif", "gif", "heic", "jpg", "png", "tiff", "webp" };
	private static List<string> TextExtensions = new() { "srt", "sub", "sup", "txt" };
	private static List<string> ComicExtensions = new() { "cb7", "cbr", "cbz" };
	private static List<string> ArchiveExtensions = new() { "zip", "7z", "rar", "gz", "tar" };
	private static List<string> AudioExtensions = new() { "aac", "flac", "m4a", "mp3", "ogg", "wav" };
	private static List<string> CodeExtensions = new() { "xml", "json", "js", "yaml" };
	private static List<string> DiscExtensions = new() { "iso", "cue", "img", "bin" };
	private static List<string> UrlExtensions = new() { "url" };
	
	private static readonly string[] COLORS = new string[]
	{
		"#006690", // a
		"#007E56", // b
		"#BB0000", // c
		"#AF3A11", // d
		"#CE104A", // e
		"#0066BB", // f
		"#FFA500", // g
		"#10B6BB", // h
		"#0066BB", // i
		"#CCB321", // j
		"#AD452A", // k
		"#008000", // l
		"#000080", // m
		"#800000", // n
		"#A52A2A", // o
		"#78016D", // p
		"#240A34", // q
		"#61BB10", // r
		"#10B6BB", // s
		"#FF4500", // t
		"#8B0000", // u
		"#00CED1", // v
		"#4682B4", // w
		"#FF69B4", // x
		"#7B68EE", // y
		"#4B0082", // z
		"#ADD8E6" // Additional color
	};

	/// <summary>
	/// Gets an extension icon
	/// </summary>
	/// <param name="extension">the extension</param>
	/// <param name="pad">the amount of padding</param>
	/// <returns>the icon</returns>
	[HttpGet("{extension}.svg")]
	public IActionResult Icon(string extension, [FromQuery] int pad = 0)
	{
		extension = extension.ToLowerInvariant();
		if (ArchiveExtensions.Contains(extension))
			return ReturnImage(ARCHIVE, extension, ArchiveExtensions.IndexOf(extension), pad);
        if (AudioExtensions.Contains(extension))
	        return ReturnImage(AUDIO, extension, AudioExtensions.IndexOf(extension), pad);
        if (ComicExtensions.Contains(extension))
	        return ReturnImage(COMIC, extension, ComicExtensions.IndexOf(extension), pad);
        if (CodeExtensions.Contains(extension))
	        return ReturnImage(CODE, extension, CodeExtensions.IndexOf(extension), pad);
        if (ImageExtensions.Contains(extension))
	        return ReturnImage(IMAGE, extension, ImageExtensions.IndexOf(extension), pad);
        if (TextExtensions.Contains(extension))
	        return ReturnImage(TEXT, extension, TextExtensions.IndexOf(extension), pad);
        if (VideoExtensions.Contains(extension))
	        return ReturnImage(VIDEO, extension, VideoExtensions.IndexOf(extension), pad);
        if (DiscExtensions.Contains(extension))
	        return ReturnImage(DISC, extension, DiscExtensions.IndexOf(extension), pad);
        if (UrlExtensions.Contains(extension))
	        return ReturnImage(URL, extension, UrlExtensions.IndexOf(extension), pad);
        if (extension == "pdf")
	        return ReturnImage(PDF, extension, 2 /* red */, pad);
        return ReturnImage(string.Empty, extension.Length > 4 ? extension[..4] : extension, pad);
	}
	
	/// <summary>
	/// Gets an padded extension icon
	/// </summary>
	/// <param name="extension">the extension</param>
	/// <param name="pad">the amount of padding</param>
	/// <returns>the icon</returns>
	[HttpGet("{pad:int}/{extension}.svg")]
	public IActionResult PaddedIcon([FromRoute]string extension, [FromRoute] int pad = 0)
	{
		extension = extension.ToLowerInvariant();
		if (ArchiveExtensions.Contains(extension))
			return ReturnImage(ARCHIVE, extension, ArchiveExtensions.IndexOf(extension), pad);
		if (AudioExtensions.Contains(extension))
			return ReturnImage(AUDIO, extension, AudioExtensions.IndexOf(extension), pad);
		if (ComicExtensions.Contains(extension))
			return ReturnImage(COMIC, extension, ComicExtensions.IndexOf(extension), pad);
		if (CodeExtensions.Contains(extension))
			return ReturnImage(CODE, extension, CodeExtensions.IndexOf(extension), pad);
		if (ImageExtensions.Contains(extension))
			return ReturnImage(IMAGE, extension, ImageExtensions.IndexOf(extension), pad);
		if (TextExtensions.Contains(extension))
			return ReturnImage(TEXT, extension, TextExtensions.IndexOf(extension), pad);
		if (VideoExtensions.Contains(extension))
			return ReturnImage(VIDEO, extension, VideoExtensions.IndexOf(extension), pad);
		if (DiscExtensions.Contains(extension))
			return ReturnImage(DISC, extension, DiscExtensions.IndexOf(extension), pad);
		if (UrlExtensions.Contains(extension))
			return ReturnImage(URL, extension, UrlExtensions.IndexOf(extension), pad);
		if (extension == "pdf")
			return ReturnImage(PDF, extension, 2 /* red */, pad);
		if(extension == "folder")
			return ReturnImage("FOLDER", extension, 1, pad);
		return ReturnImage(string.Empty, extension.Length > 4 ? extension[..4] : extension, pad: pad);
	}
	
	/// <summary>
	/// Returns an image
	/// </summary>
	/// <param name="icon">the icon to return</param>
	/// <param name="extension">the file extension</param>
	/// <param name="index">the index of the color</param>
	/// <param name="pad">the amount of padding</param>
	/// <returns>the result</returns>
	private IActionResult ReturnImage(string icon, string extension, int index = -1, int pad = 0)
	{
		while (index > COLORS.Length)
			index -= COLORS.Length;
		string color = index >= 0 ? COLORS[index] : GetColor(extension);

		// Convert pad percentage to a scale factor
		double scale = 1 - (pad / 100.0);
		scale = Math.Max(0, Math.Min(1, scale)); // Ensure scale is between 0 and 1

		// Compute translation to center the scaled content
		double translate = (1 - scale) * 28; // 28 is half of the viewBox size (56/2)

		// Modify HEAD to apply the transformation inside the <g> tag
		string transformedHead = HEAD.Replace("<g>", $"<g transform=\"translate({translate}, {translate}) scale({scale})\">");

		string svg;
		if (icon == "FOLDER")
		{
			svg = $@"
<?xml version=""1.0"" encoding=""iso-8859-1""?>
<svg width=""800px"" height=""800px"" viewBox=""0 0 1024 1024"" class=""icon"" version=""1.1"" xmlns=""http://www.w3.org/2000/svg"">
    <g transform=""translate(512,512) scale({scale}) translate(-512,-512)"">
        <path fill=""#666"" d=""M853.333333 256H469.333333l-85.333333-85.333333H170.666667c-46.933333 0-85.333333 38.4-85.333334 85.333333v170.666667h853.333334v-85.333334c0-46.933333-38.4-85.333334-85.333334-85.333333z"" />
        <path fill=""#404040"" d=""M853.333333 256H170.666667c-46.933333 0-85.333333 38.4-85.333334 85.333333v426.666667c0 46.933333 38.4 85.333333 85.333334 85.333333h682.666666c46.933333 0 85.333333-38.4 85.333334-85.333333V341.333333c0-46.933333-38.4-85.333333-85.333334-85.333333z"" />
    </g>
</svg>
".Trim();
		}
		else
		{
			svg = $@"
{transformedHead}
{icon.Replace("{COLOR}", color)}
{Bottom(extension, color)}
{END}".Trim();
		}

		return new ContentResult
		{
			Content = svg,
			ContentType = "image/svg+xml",
		};
	}


	/// <summary>
	/// Gets the bottom with the extension
	/// </summary>
	/// <param name="extension">the extension</param>
	/// <param name="color">the color</param>
	/// <returns>the bottom</returns>
	private string Bottom(string extension, string color)
	{
		return $"<path style=\"fill:{color};\" d=\"M48.037,56H7.963C7.155,56,6.5,55.345,6.5,54.537V39h43v15.537C49.5,55.345,48.845,56,48.037,56z\"/>" +
			   $"<text x=\"49%\" y=\"52\" text-anchor=\"middle\">{extension.ToUpper()}</text>";
		
	}


	/// <summary>
	/// Get the color x for the string. If the first character is a letter, returns its corresponding color. Otherwise, returns the additional color.
	/// </summary>
	/// <param name="input">The input string</param>
	/// <returns>The color</returns>
	static string GetColor(string input)
	{
		// Convert the first character of the string to lowercase
		char firstChar = Char.ToLower(input[0]);

		// If the character is a letter, return its index, otherwise return the index of the additional color
		if (firstChar is >= 'a' and <= 'z')
		{
			return COLORS[firstChar - 'a'];
		}
		else
		{
			return COLORS.Last(); // Index of the additional color
		}
	}
}