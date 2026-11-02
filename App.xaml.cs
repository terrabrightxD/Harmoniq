using System.IO;
using System.Text.Json;
using System.Windows;
namespace CrateScope;
public partial class App:Application {
 protected override void OnStartup(StartupEventArgs e){base.OnStartup(e);
  if(e.Args.Length>=3&&e.Args[0]=="--analyze"){
   var results=new List<Track>();var errors=new List<string>();foreach(var file in e.Args.Skip(2)){try{results.Add(Engine.Analyze(file));}catch(Exception ex){errors.Add(file+": "+ex.Message);}}
   using var output=new FileStream(e.Args[1],FileMode.CreateNew);JsonSerializer.Serialize(output,new{results,errors},new JsonSerializerOptions{WriteIndented=true});Shutdown(errors.Count>0?1:0);return;
  }
  var window=new MainWindow();window.Show();
  if(e.Args.Length==2&&e.Args[0]=="--preview")window.Preview(e.Args[1]);
 }
}
