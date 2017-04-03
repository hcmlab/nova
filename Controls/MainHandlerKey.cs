using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi
{
    public partial class MainHandler
    {

        public void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.control.annoListControl.editTextBox.IsFocused)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.Space))
                {
                    handlePlay();

                    e.Handled = true;
                }


                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.L))
                {
                    if (AnnoTierStatic.Selected != null && Properties.Settings.Default.CMLDefaultStream != null)
                    {
                        DatabaseHandler.StoreToDatabase(AnnoTierStatic.Selected.AnnoList, loadedDBmedia, false);
                        CompleteTier(Properties.Settings.Default.CMLContext, AnnoTierStatic.Selected, Properties.Settings.Default.CMLDefaultStream, Properties.Settings.Default.CMLDefaultConf, Properties.Settings.Default.CMLDefaultGap, Properties.Settings.Default.CMLDefaultMinDur);
                    }

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.D0) || e.KeyboardDevice.IsKeyDown(Key.NumPad0))
                {
                    if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE || AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                    {
                        string label = "GARBAGE";
                        if (AnnoTierStatic.Label != null)
                        {
                            AnnoTierStatic.Label.Item.Label = label;
                            AnnoTierStatic.Label.Item.Color = Colors.Black;
                            AnnoTierStatic.Label.Item.Confidence = 1.0;
                        }
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.D1) || e.KeyboardDevice.IsKeyDown(Key.D2) || e.KeyboardDevice.IsKeyDown(Key.D3) || e.KeyboardDevice.IsKeyDown(Key.D4) ||
                    e.KeyboardDevice.IsKeyDown(Key.D5) || e.KeyboardDevice.IsKeyDown(Key.D6) || e.KeyboardDevice.IsKeyDown(Key.D7) || e.KeyboardDevice.IsKeyDown(Key.D8) || e.KeyboardDevice.IsKeyDown(Key.D9) ||
                    e.KeyboardDevice.IsKeyDown(Key.NumPad1) || e.KeyboardDevice.IsKeyDown(Key.NumPad2) || e.KeyboardDevice.IsKeyDown(Key.NumPad3) || e.KeyboardDevice.IsKeyDown(Key.NumPad4) || e.KeyboardDevice.IsKeyDown(Key.NumPad5) ||
                    e.KeyboardDevice.IsKeyDown(Key.NumPad6) || e.KeyboardDevice.IsKeyDown(Key.NumPad7) || e.KeyboardDevice.IsKeyDown(Key.NumPad8) || e.KeyboardDevice.IsKeyDown(Key.NumPad9))
                {
                    int index = 0;
                    if (e.Key - Key.D1 < 10 && AnnoTierStatic.Selected.AnnoList.Scheme.Labels != null && AnnoTierStatic.Selected.AnnoList.Scheme.Labels.Count > 0) index = Math.Min(AnnoTierStatic.Selected.AnnoList.Scheme.Labels.Count - 1, e.Key - Key.D1);
                    else if (AnnoTierStatic.Selected.AnnoList.Scheme.Labels != null) index = Math.Min(AnnoTierStatic.Selected.AnnoList.Scheme.Labels.Count - 1, e.Key - Key.NumPad1);

                    if (index >= 0 && AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                    {
                        string label = AnnoTierStatic.Selected.AnnoList.Scheme.Labels.ElementAt(index).Name;
                        if (AnnoTierStatic.Label != null)
                        {
                            AnnoTierStatic.Label.Item.Label = label;
                            AnnoTierStatic.Selected.DefaultLabel = label;

                            foreach (AnnoScheme.Label lp in AnnoTierStatic.Selected.AnnoList.Scheme.Labels)
                            {
                                if (label == lp.Name)
                                {
                                    AnnoTierStatic.Label.Item.Color = lp.Color;
                                    AnnoTierStatic.Label.Item.Confidence = 1.0;
                                    AnnoTierStatic.Selected.DefaultColor = lp.Color;

                                    break;
                                }
                            }
                        }
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.Right) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (AnnoTierStatic.Label != null && AnnoTierStatic.Selected.IsDiscreteOrFree && isKeyDown == false /*&& AnnoTierStatic.Label == null*/)
                    {
                        UIElement container = VisualTreeHelper.GetParent(AnnoTierStatic.Label) as UIElement;
                        Point relativeLocation = AnnoTierStatic.Label.TranslatePoint(new Point(0, 0), container);

                        mediaList.Move(Time.TimeFromPixel(relativeLocation.X + AnnoTierStatic.Label.Width));

                        if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        {
                            annoCursor.X = relativeLocation.X + AnnoTierStatic.Label.Width;
                        }
                        else signalCursor.X = relativeLocation.X + AnnoTierStatic.Label.Width;

                        timeline.CurrentSelectPosition = annoCursor.X;
                        timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                        timeline.CurrentPlayPositionPrecise = Time.TimeFromPixel(signalCursor.X);
                        AnnoTierStatic.Label.select(true);
                        isKeyDown = true;
                    }
                    e.Handled = true;
                }

                else if (e.KeyboardDevice.IsKeyDown(Key.Right) && !e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && !e.KeyboardDevice.IsKeyDown(Key.LeftAlt) && !isKeyDown)
                {

                    isKeyDown = true;
                    if (AnnoTierStatic.Selected != null) AnnoTierStatic.Selected.Focus();
                    int i = 0;
                    double fps = 1.0 / 25.0;
                    foreach (IMedia media in mediaList)
                    {
                        if (media.GetMediaType() == MediaType.VIDEO) break;
                        i++;
                    }

                    if (i < mediaList.Count)
                    {
                        fps = 1.0 / mediaList[i].GetSampleRate();
                    }

                    mediaList.Move(Time.TimeFromPixel(signalCursor.X) + fps);
                    timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X) + fps;
                    timeline.CurrentPlayPositionPrecise = Time.TimeFromPixel(signalCursor.X) + fps;
                    double pos = Time.PixelFromTime(timeline.CurrentPlayPosition);
                    signalCursor.X = pos;


                    if (Time.CurrentPlayPosition >= Time.SelectionStop && control.navigator.followplaybox.IsChecked == true)
                    {
                        double factor = (((timeline.CurrentPlayPosition - timeline.SelectionStart) / (timeline.SelectionStop - timeline.SelectionStart)));
                        control.timeLineControl.rangeSlider.followmedia = true;
                        control.timeLineControl.rangeSlider.MoveAndUpdate(true, factor);

                        if (timeline.SelectionStop - timeline.SelectionStart < 1) timeline.SelectionStart = timeline.SelectionStop - 1;
                        signalCursor.X = 1;


                    }
                    else if (control.navigator.followplaybox.IsChecked == false) control.timeLineControl.rangeSlider.followmedia = false;
                
                    double time = Time.TimeFromPixel(pos);
                    updatePositionLabels(time);
                    control.annoTierControl.currentTime = time;
                    e.Handled = true;

                }




                if (e.KeyboardDevice.IsKeyDown(Key.Left) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (AnnoTierStatic.Label != null && AnnoTierStatic.Selected.IsDiscreteOrFree && isKeyDown == false /*&& AnnoTierStatic.Label == null*/)
                    {
                        UIElement container = VisualTreeHelper.GetParent(AnnoTierStatic.Label) as UIElement;
                        Point relativeLocation = AnnoTierStatic.Label.TranslatePoint(new Point(0, 0), container);

                        mediaList.Move(Time.TimeFromPixel(relativeLocation.X));

                        if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        {
                            annoCursor.X = relativeLocation.X;
                        }
                        else signalCursor.X = relativeLocation.X;

                        timeline.CurrentSelectPosition = annoCursor.X;
                        timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                        timeline.CurrentPlayPositionPrecise = Time.TimeFromPixel(signalCursor.X);
                        AnnoTierStatic.Label.select(true);
                        isKeyDown = true;
                    }
                    e.Handled = true;
                }

                else if (e.KeyboardDevice.IsKeyDown(Key.Left) && !e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && !e.KeyboardDevice.IsKeyDown(Key.LeftAlt) && !isKeyDown)
                {

                     isKeyDown = true;
                    if (AnnoTierStatic.Selected != null) AnnoTierStatic.Selected.Focus();
                    int i = 0;
                    double fps = 1.0 / 25.0;
                    foreach (IMedia im in mediaList)
                    {
                        if (im.GetMediaType() == MediaType.VIDEO) break;
                        i++;
                    }

                    if (i < mediaList.Count)
                    {
                        fps = 1.0 / mediaList[i].GetSampleRate();
                    }

                    mediaList.Move(Time.TimeFromPixel(signalCursor.X) - fps);
                    timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X) - fps;
                    timeline.CurrentPlayPositionPrecise = timeline.CurrentPlayPosition;
                    double pos = Time.PixelFromTime(timeline.CurrentPlayPosition);
                    signalCursor.X = pos;

                    if (Time.CurrentPlayPosition < Time.SelectionStart && Time.SelectionStart > 0 && control.navigator.followplaybox.IsChecked == true)
                    {
                        double factor = (((timeline.SelectionStop - timeline.CurrentPlayPosition) / (timeline.SelectionStop - timeline.SelectionStart)));
                        control.timeLineControl.rangeSlider.followmedia = true;
                        control.timeLineControl.rangeSlider.MoveAndUpdate(false, factor);

                        if (timeline.SelectionStop - timeline.SelectionStart < 1) timeline.SelectionStart = timeline.SelectionStop - 1;
                        signalCursor.X = Time.PixelFromTime(Time.SelectionStop);

                    }
                    else if (control.navigator.followplaybox.IsChecked == false) control.timeLineControl.rangeSlider.followmedia = false;
                   
                    double time = Time.TimeFromPixel(pos);
                    if (time != 0)
                    {
                        updatePositionLabels(time);
                        control.annoTierControl.currentTime = Time.TimeFromPixel(pos);
                    }

                    e.Handled = true;
                }



                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && !isKeyDown)
                {
                    if (AnnoTierStatic.Selected != null && !AnnoTierStatic.Selected.IsDiscreteOrFree)
                    {
                        AnnoTierStatic.Selected.ContinuousAnnoMode();
                    }
                    isKeyDown = true;
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.T) && e.KeyboardDevice.IsKeyDown(Key.Down))
                {
                    for (int i = 0; i < annoTiers.Count; i++)
                    {
                        if (annoTiers[i] == AnnoTierStatic.Selected && i + 1 < annoTiers.Count)
                        {
                            AnnoTierStatic.Select(annoTiers[i + 1]);
                            AnnoTierStatic.SelectLabel(null);
                            break;
                        }
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.T) && e.KeyboardDevice.IsKeyDown(Key.Up))
                {
                    for (int i = 0; i < annoTiers.Count; i++)
                    {
                        if (annoTiers[i] == AnnoTierStatic.Selected && i > 0)
                        {
                            AnnoTierStatic.Select(annoTiers[i - 1]);
                            AnnoTier.SelectLabel(null);
                            break;
                        }
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.C) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.IsDiscreteOrFree)
                {
                    if (AnnoTierStatic.Label != null)
                    {
                        temp_segment = AnnoTierStatic.Label;
                        AnnoTierStatic.Label.select(false);
                    }

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.X) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.IsDiscreteOrFree)
                {
                    if (AnnoTierStatic.Label != null)
                    {
                        temp_segment = AnnoTierStatic.Label;
                        AnnoTierStatic.OnKeyDownHandler(sender, e);
                    }

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.V) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.IsDiscreteOrFree)
                {
                    if (AnnoTierStatic.Selected != null)
                    {
                        double start = Time.TimeFromPixel(annoCursor.X);
                        if (temp_segment != null) AnnoTierStatic.Selected.NewAnnoCopy(start, start + temp_segment.Item.Duration, temp_segment.Item.Label, temp_segment.Item.Color, temp_segment.Item.Confidence);
                    }

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.Z))
                {
                    if (AnnoTierStatic.Selected != null)
                    {
                        AnnoTierStatic.Selected.UnDoObject.Undo(1);
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.Y))
                {
                    if (AnnoTierStatic.Selected != null)
                    {
                        AnnoTierStatic.Selected.UnDoObject.Redo(1);
                    }

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) && e.KeyboardDevice.IsKeyDown(Key.Down))
                {
                    if (AnnoTierStatic.Label != null)
                    {
                        AnnoListItem temp = AnnoTierStatic.Label.Item;

                        for (int i = 0; i < annoTiers.Count; i++)
                        {
                            if (annoTiers[i] == AnnoTierStatic.Selected && i + 1 < annoTiers.Count)
                            {
                                AnnoTierStatic.Select(annoTiers[i + 1]);
                                AnnoTier.SelectLabel(null);
                                if (!AnnoTierStatic.Selected.AnnoList.Contains(temp)) AnnoTierStatic.Selected.NewAnnoCopy(temp.Start, temp.Stop, temp.Label, temp.Color);

                                break;
                            }
                        }
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) && e.KeyboardDevice.IsKeyDown(Key.Up))
                {
                    if (AnnoTierStatic.Label != null)
                    {
                        AnnoListItem temp = AnnoTierStatic.Label.Item;

                        for (int i = 0; i < annoTiers.Count; i++)
                        {
                            if (annoTiers[i] == AnnoTierStatic.Selected && i > 0)
                            {
                                AnnoTierStatic.Select(annoTiers[i - 1]);
                                AnnoTierStatic.SelectLabel(null);
                                if (!AnnoTierStatic.Selected.AnnoList.Contains(temp)) AnnoTierStatic.Selected.NewAnnoCopy(temp.Start, temp.Stop, temp.Label, temp.Color);
                                break;
                            }
                        }
                    }
                    e.Handled = true;
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!control.annoListControl.editTextBox.IsFocused)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.S) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (DatabaseLoaded)
                    {
                        databaseStore();
                    }
                    else saveSelectedAnno();
                }
                else if (e.KeyboardDevice.IsKeyDown(Key.Delete) || e.KeyboardDevice.IsKeyDown(Key.Back))
                {
                    if (AnnoTierStatic.Label != null)
                    {
                        AnnoTierStatic.OnKeyDownHandler(sender, e);
                    }
                }

                if (e.KeyboardDevice.IsKeyDown(Key.R) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0 && AnnoTierStatic.Selected.AnnoList.Scheme.SampleRate != Properties.Settings.Default.DefaultDiscreteSampleRate)
                    {
                        foreach (AnnoListItem ali in AnnoTierStatic.Selected.AnnoList)
                        {
                            if (ali.Start % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                            {
                                int round = (int)(ali.Start / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                ali.Start = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                            }

                            if (ali.Stop % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                            {
                                int round = (int)(ali.Stop / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                ali.Stop = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                            }

                            ali.Duration = ali.Stop - ali.Start;
                        }
                        AnnoTierStatic.Selected.AnnoList.Scheme.SampleRate = Properties.Settings.Default.DefaultDiscreteSampleRate;
                    }
                }

                if (e.KeyboardDevice.IsKeyDown(Key.R) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.F5))
                {
                    AnnoTier track = AnnoTierStatic.Selected;
                    if (track != null)
                    {
                        if (DatabaseLoaded)
                            databaseReload(track);
                        else
                            reloadAnnoTier(track.AnnoList.Source.File.Path);
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.E) && !isKeyDown)
                {
                    if (AnnoTierStatic.Label != null && AnnoTierStatic.Selected.IsDiscreteOrFree && isKeyDown == false)
                    {
                        UIElement container = VisualTreeHelper.GetParent(AnnoTierStatic.Label) as UIElement;
                        Point relativeLocation = AnnoTierStatic.Label.TranslatePoint(new Point(0, 0), container);

                        mediaList.Move(Time.TimeFromPixel(relativeLocation.X + AnnoTierStatic.Label.Width));

                        annoCursor.X = relativeLocation.X;
                        signalCursor.X = relativeLocation.X + AnnoTierStatic.Label.Width;

                        timeline.CurrentSelectPosition = annoCursor.X;
                        timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                        AnnoTierStatic.Label.select(true);
                        isKeyDown = true;
                    }
                }

                if (e.KeyboardDevice.IsKeyDown(Key.Q) && !isKeyDown)
                {
                    if (AnnoTierStatic.Label != null && AnnoTierStatic.Selected.IsDiscreteOrFree && isKeyDown == false)
                    {
                        UIElement container = VisualTreeHelper.GetParent(AnnoTierStatic.Label) as UIElement;
                        Point relativeLocation = AnnoTierStatic.Label.TranslatePoint(new Point(0, 0), container);

                        mediaList.Move(Time.TimeFromPixel(relativeLocation.X));

                        annoCursor.X = relativeLocation.X + AnnoTierStatic.Label.Width;
                        signalCursor.X = relativeLocation.X;

                        timeline.CurrentSelectPosition = annoCursor.X;
                        timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                        AnnoTierStatic.Label.select(true);
                        isKeyDown = true;
                    }
                }

                if ((e.KeyboardDevice.IsKeyDown(Key.W) || !isKeyDown && AnnoTierStatic.Selected != null) && AnnoTierStatic.Selected.IsDiscreteOrFree && !e.KeyboardDevice.IsKeyDown(Key.LeftAlt) && !e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                {
                    if (AnnoTierStatic.Label == null)
                    {
                        AnnoTierStatic.Selected.NewAnnoKey();
                    }
                    else
                    {
                        ShowLabelBox();
                    }
                    if (AnnoTierStatic.Label != null) AnnoTierStatic.Label.select(true);
                    isKeyDown = true;
                }
                else if ((e.KeyboardDevice.IsKeyDown(Key.W) || !isKeyDown && AnnoTierStatic.Selected != null) && !AnnoTierStatic.Selected.IsDiscreteOrFree)
                {
                    if (AnnoTierStatic.Label != null)
                    {
                        ShowLabelBoxCont();
                    }
                    if (AnnoTierStatic.Label != null) AnnoTierStatic.Label.select(true);
                    isKeyDown = true;
                }
                if (e.KeyboardDevice.IsKeyDown(Key.Right) && e.KeyboardDevice.IsKeyDown(Key.LeftAlt) /*&& !isKeyDown*/)
                {
                    int i = 0;
                    double fps = 1.0 / 30.0;
                    foreach (IMedia im in mediaList)
                    {
                        if (im.GetMediaType() == MediaType.VIDEO)
                        {
                            break;
                        }
                        i++;
                    }

                    if (i < mediaList.Count)
                    {
                        fps = 1.0 / mediaList[i].GetSampleRate();
                    }

                    //In case no media is loaded it takes the sr of the first loaded signal
                    else
                    {
                        if (signals.Count > 0)
                        {
                            fps = 1.0 / signals[0].rate;
                        }
                    }

                    mediaList.Move(Time.TimeFromPixel(signalCursor.X) + fps);

                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                    {
                        annoCursor.X = annoCursor.X + Time.PixelFromTime(fps);
                    }
                    else signalCursor.X = signalCursor.X + Time.PixelFromTime(fps);

                    timeline.CurrentSelectPosition = annoCursor.X;
                    timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                    timeline.CurrentPlayPositionPrecise = Time.TimeFromPixel(signalCursor.X);

                    if (AnnoTierStatic.Label != null)
                    {
                        double start = annoCursor.X;
                        double end = signalCursor.X;

                        if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        {
                            start = signalCursor.X;
                            end = annoCursor.X;
                        }
                        if (end > start)
                        {
                            AnnoTierStatic.Label.resize_right(Time.PixelFromTime(fps));
                        }
                        else
                        {
                            AnnoTierStatic.Label.resize_left(Time.PixelFromTime(fps));
                        }
                        AnnoTierStatic.Label.select(true);
                    }

                    isKeyDown = true;
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.Left) && e.KeyboardDevice.IsKeyDown(Key.LeftAlt)/* && !isKeyDown*/)
                {
                    int i = 0;
                    double fps = 1.0 / 30.0;
                    foreach (IMedia im in mediaList)
                    {
                        if (im.GetMediaType() == MediaType.VIDEO)
                        {
                            break;
                        }
                        i++;
                    }

                    if (i < mediaList.Count)
                    {
                        fps = 1.0 / mediaList[i].GetSampleRate();
                    }

                    mediaList.Move(Time.TimeFromPixel(signalCursor.X) - fps);
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                    {
                        annoCursor.X = annoCursor.X - Time.PixelFromTime(fps);
                    }
                    else
                    {
                        signalCursor.X = signalCursor.X - Time.PixelFromTime(fps);
                    }

                    timeline.CurrentSelectPosition = annoCursor.X;
                    timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                    timeline.CurrentPlayPositionPrecise = Time.TimeFromPixel(signalCursor.X);

                    double start = annoCursor.X;
                    double end = signalCursor.X;

                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                    {
                        start = signalCursor.X;
                        end = annoCursor.X;
                    }

                    if (AnnoTierStatic.Label != null)
                    {
                        if (end > start)
                        {
                            AnnoTierStatic.Label.resize_right(-Time.PixelFromTime(fps));
                        }
                        else
                        {
                            AnnoTierStatic.Label.resize_left(-Time.PixelFromTime(fps));
                        }
                        AnnoTierStatic.Label.select(true);
                    }

                    isKeyDown = true;
                    e.Handled = true;
                }
                else if(!e.KeyboardDevice.IsKeyDown(Key.LeftAlt))
                {
                    AnnoTierStatic.OnKeyDownHandler(sender, e);
                }
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            isKeyDown = false;
        }
    }
}
