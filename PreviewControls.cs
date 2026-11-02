using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace CrateScope;
public partial class MainWindow {
 public void RunCueTest(string output){if(auditionTrack==null)throw new Exception("Testtrack fehlt.");auditionTrack.Cues=[];SeekTo(10);SetCue(this,new RoutedEventArgs());if(auditionTrack.Cues.Length!=1||auditionTrack.Cues[0]!=10)throw new Exception("Cue wurde nicht gesetzt.");SeekTo(0);CueList.SelectedItem=10.0;if(Math.Abs(playhead-10)>.01)throw new Exception("Cue-Sprung fehlgeschlagen.");using var file=new System.IO.FileStream(output,System.IO.FileMode.CreateNew);System.Text.Json.JsonSerializer.Serialize(file,new{cue=auditionTrack.Cues[0],position=playhead,passed=true});}
 Player? player;Track? auditionTrack;bool loadingPlayer,updatingPosition;int auditionGeneration;double playhead;
 readonly DispatcherTimer playbackTimer=new(){Interval=TimeSpan.FromMilliseconds(50)};
 void SetupPreview(){playbackTimer.Tick+=(_,_)=>{if(player!=null){playhead=player.Position;UpdatePosition();PlayButton.Content=player.Playing?"Ⅱ Pause":"▶ Abspielen";}};playbackTimer.Start();Closed+=(_,_)=>{auditionGeneration++;playbackTimer.Stop();player?.Dispose();};}
 void ChangePreview(Track track){if(ReferenceEquals(auditionTrack,track))return;auditionGeneration++;player?.Dispose();player=null;auditionTrack=track;playhead=0;loadingPlayer=false;PlayButton.IsEnabled=true;PlayButton.Content="▶ Abspielen";PositionSlider.Maximum=track.Duration;CueList.ItemsSource=track.Cues;UpdatePosition();}
 void UpdatePosition(){updatingPosition=true;PositionSlider.Value=playhead;TimeLabel.Text=$@"{TimeSpan.FromSeconds(playhead):mm\:ss\.ff} / {TimeSpan.FromSeconds(auditionTrack?.Duration??0):mm\:ss}";Wave.Position=playhead;Wave.InvalidateVisual();updatingPosition=false;}
 async void TogglePlay(object sender,RoutedEventArgs args){if(auditionTrack==null||loadingPlayer)return;try{if(player==null){int generation=auditionGeneration;string path=auditionTrack.Path;loadingPlayer=true;PlayButton.IsEnabled=false;Status.Text="Audio für Vorschau laden …";var decoded=await Task.Run(()=>AudioSource.Open(path));if(generation!=auditionGeneration){decoded.Dispose();return;}try{player=new Player(decoded);}finally{loadingPlayer=false;PlayButton.IsEnabled=true;}player.Volume=(float)VolumeSlider.Value;player.Position=playhead;}if(player.Playing)player.Pause();else player.Play();PlayButton.Content=player.Playing?"Ⅱ Pause":"▶ Abspielen";Status.Text="Vorschau · Waveform anklicken zum Springen";}catch(Exception ex){loadingPlayer=false;PlayButton.IsEnabled=true;Status.Text="Vorschau nicht verfügbar: "+ex.Message;}}
 void StopPlayback(object sender,RoutedEventArgs e){player?.Pause();SeekTo(0);PlayButton.Content="▶ Abspielen";}
 void SeekTo(double position){if(auditionTrack==null)return;playhead=Math.Clamp(position,0,auditionTrack.Duration);if(player!=null)player.Position=playhead;UpdatePosition();}
 void SliderSeek(object sender,RoutedPropertyChangedEventArgs<double> e){if(!updatingPosition&&PositionSlider!=null)SeekTo(e.NewValue);}
 void WaveSeek(object sender,MouseButtonEventArgs e){if(auditionTrack!=null&&Wave.ActualWidth>0)SeekTo(e.GetPosition(Wave).X/Wave.ActualWidth*auditionTrack.Duration);}
 void VolumeChange(object sender,RoutedPropertyChangedEventArgs<double> e){if(player!=null)player.Volume=(float)e.NewValue;}
 void JumpCue(object sender,SelectionChangedEventArgs e){if(CueList.SelectedItem is double time)SeekTo(time);}
 void SetCue(object sender,RoutedEventArgs e){if(auditionTrack==null)return;var t=auditionTrack;if(t.Cues.Length>=8){Status.Text="Maximal 8 Cues. Erst einen Cue entfernen.";return;}if(playhead>=t.Duration||t.Cues.Any(c=>Math.Abs(c-playhead)<.05)){Status.Text="Hier kann kein zusätzlicher Cue gesetzt werden.";return;}t.Cues=t.Cues.Append(Math.Round(playhead,3)).Order().ToArray();PersistCues();}
 void RemoveCue(object sender,RoutedEventArgs e){if(auditionTrack==null||CueList.SelectedItem is not double time)return;auditionTrack.Cues=auditionTrack.Cues.Where(c=>c!=time).ToArray();PersistCues();}
 void PersistCues(){if(auditionTrack==null)return;auditionTrack.Manual=true;CueEdit.Text=string.Join("; ",auditionTrack.Cues.Select(c=>c.ToString("F3",CultureInfo.InvariantCulture)));CueList.ItemsSource=auditionTrack.Cues;Wave.InvalidateVisual();try{Save();Status.Text="Cue-Änderung gespeichert";}catch(Exception ex){Status.Text="Cue noch nicht gespeichert: "+ex.Message;}}
}

