import { Tester } from '../Shared/tester.js';

/**
* Checks if a file is older than the specified days 
* @author John Andrews 
* @revision 1
* @param {int} a The number of days to check how old the file is 
* @param {int} b If the last write time should be used, otherwise the creation time will be 
* @output The file is older than the days specified 
* @output the file is not older than the days specified
*/
function Script(a, b)
{
    //var t = require('../Shared/Tester');

    //var console = require('console');
    //console.WriteLine('Test from System.Console!');
    let t = new Tester();
    let multiple = t.multiple(a, b);
    //console.log(multiple);
    Logger.ILog('multiple: ', multiple);

    t.list('a', 'b', 'c', 'd', 'e', 123);

    var file = new System.IO.StreamWriter('log.txt');
    file.WriteLine('Hello World !');
    file.Dispose();
    
	return a > b ? 1 : 2;
}