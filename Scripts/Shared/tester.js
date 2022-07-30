/**
 * Test class not used
 * @name Tester
 * @revision 2
 * @minimumVersion 1.0.0.0
 */
export class Tester {
    print() {
        console.log('this is a test');        
    }

    multiple(a, b){
        return a * b;
    }

    list(){
        var ListOfString = System.Collections.Generic.List(System.String);
        var list = new ListOfString();
        for(let arg of arguments){
            list.Add('' + arg);
        }
        Logger.ILog('List count: ' + list.Count);
    }
}