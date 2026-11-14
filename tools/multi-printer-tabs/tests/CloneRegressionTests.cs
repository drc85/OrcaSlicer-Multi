using System;
using System.IO;
using System.Reflection;
using OrcaMultiPrinterTabs;
class CloneRegressionTests {
    [STAThread] static int Main() {
        string root=Path.Combine(Environment.CurrentDirectory,"work","clone-check-"+Guid.NewGuid().ToString("N").Substring(0,8));
        string source=Path.Combine(root,"source"),target=Path.Combine(root,"target");
        string[] required={"python/packages/cp312/example/__init__.py","orca_plugins/example/textures/image.png","orca_plugins/example/__whl_extracted__/package/__init__.py","orca_plugins/example/cache/resource.txt"};
        foreach(string relative in required){string path=Path.Combine(source,relative);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,relative);}
        Directory.CreateDirectory(Path.Combine(source,"log"));File.WriteAllText(Path.Combine(source,"log","old.log"),"cache");
        using(var host=new Host()) {
            typeof(Host).GetMethod("CopyDirectory",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(host,new object[]{source,target,true});
        }
        foreach(string relative in required)if(File.ReadAllText(Path.Combine(target,relative))!=relative)throw new Exception("Missing plugin asset: "+relative);
        if(Directory.Exists(Path.Combine(target,"log")))throw new Exception("Application logs should not be copied");
        Console.WriteLine("PASS: Python packages, texture assets, extracted wheels and nested resource folders preserved; application logs omitted.");
        return 0;
    }
}
