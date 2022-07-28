/**
 * Checks if a file is larger than 1 GB
 * @revision 1
 * @outputs 2
 */

if(Variables.file.Size > 1_000_000_000)
	return 1;
return 2;