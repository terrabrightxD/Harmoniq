using System.IO;
using System.Text.Json;
namespace CrateScope;
public static class PlaybackTest {
 public static void Run(string input,string output){using var player=new Player(AudioSource.Open(input));player.Volume=0;player.Play();Thread.Sleep(1400);long bytes=player.OutputBytes;double first=player.Position;player.Pause();player.Position=Math.Min(10,player.Duration/2);double seek=player.Position;player.Play();Thread.Sleep(500);player.Pause();double after=player.Position;if(bytes<=0||first<=0||after<=seek)throw new Exception("Wiedergabe/Seek nicht erfolgreich.");using var file=new FileStream(output,FileMode.CreateNew);JsonSerializer.Serialize(file,new{first,bytes,seek,after,passed=true});}
}
