/**
 * Checks if a file is larger than 1 GB
 * @revision 2
 * @outputs 2
 * @minimumVersion 1.0.0.0
 */

if(Variables.file.Size > 1_000_000_000)
	return 1;
return 2;