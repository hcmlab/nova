using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SharpDX.XInput;
using System.Threading;
using System.Windows.Threading;
using System.Runtime.InteropServices;



namespace ssi
{
   

    public partial class MainHandler
    {

        Controller[] controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
        Controller controller = null;
        Gamepad gamepad;
        Gamepad gamepad_last;
        public Point leftThumb, rightThumb = new Point(0, 0);
        float leftTrigger, rightTrigger;
        public int deadband = 2500;

        public void connectController()
        {
            foreach (var selectControler in controllers)
            {
                if (selectControler.IsConnected)
                {
                    controller = selectControler;
                   
                    break;
                }
            }
            if (controller == null)
            {
                Console.WriteLine("No XInput controller installed");
                if(AnnoTierStatic.Selected != null) AnnoTierStatic.Selected.ControllerConnected = false;
            }

            else
            {
                Console.WriteLine("XInput controller installed");
                if (AnnoTierStatic.Selected != null) AnnoTierStatic.Selected.ControllerConnected = true;
                var ts = new ThreadStart(CheckControllerInput);
                var backgroundThread = new Thread(ts);
                backgroundThread.Start();

            }

           

        }

        private void CheckControllerInput()
        {

        var previousState = controller.GetState();
            bool wasnotlivemode = false; ;
            bool locklivemode = false;
            while (controller.IsConnected)
            {
                var state = controller.GetState();

                {

                    gamepad = controller.GetState().Gamepad;
                    gamepad_last = previousState.Gamepad;

                    leftThumb.X = (Math.Abs((float)gamepad.LeftThumbX) < deadband) ? 0 : (float)gamepad.LeftThumbX / short.MinValue * -100;
                    leftThumb.Y = (Math.Abs((float)gamepad.LeftThumbY) < deadband) ? 0 : (float)gamepad.LeftThumbY / short.MaxValue * 100;
                    rightThumb.X = (Math.Abs((float)gamepad.RightThumbX) < deadband) ? 0 : (float)gamepad.RightThumbX / short.MaxValue * 100;
                    rightThumb.Y = (Math.Abs((float)gamepad.RightThumbY) < deadband) ? 0 : (float)gamepad.RightThumbY / short.MaxValue * 100;

                    leftTrigger = gamepad.LeftTrigger > 15 ? gamepad.LeftTrigger  : 0 ;
                    rightTrigger = gamepad.RightTrigger >  15 ? gamepad.RightTrigger : 0;

                    if (AnnoTier.Selected != null && AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                    {

                        AnnoTierStatic.Selected.LstickY = leftThumb.Y;
                        AnnoTierStatic.Selected.ControllerConnected = true;

                        System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                            double offset = (timeline.SelectionStop - timeline.SelectionStart) / 100.0;

                            if (timeline.CurrentPlayPosition > timeline.SelectionStart + offset && leftTrigger > 0)
                            {
                                if (!locklivemode)
                                {
                                    wasnotlivemode = AnnoTierStatic.isNotLiveAnnoMode;
                                    System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                                        control.annoLiveModeCheckBox.IsChecked = false;
                                        if(AnnoTierStatic.isNotLiveAnnoMode) AnnoTierStatic.Selected.LiveAnnoMode(true);
                                    }));
                                   
                                    locklivemode = true;
                                }
                               
                                signalCursor.X = signalCursor.X - leftTrigger / 10.0;
                                


                            }

                            else if (timeline.CurrentPlayPosition < timeline.SelectionStart + offset && leftTrigger > 0)
                            {
                               
                                double factor = (Time.SelectionStop - Time.SelectionStart) / Time.TotalDuration;
                                control.timeLineControl.rangeSlider.MoveAndUpdate(false, factor);
                                signalCursor.X = signalCursor.X - leftTrigger / 10.0;



                            }

                            else if(leftTrigger == 0 && wasnotlivemode)
                            {
                            
                              
                                if(locklivemode)
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                                        ToggleLiveMode();
                                    }));
                                    locklivemode = false;
                                }
                              
                            }

                           

                            if (timeline.CurrentPlayPosition < timeline.SelectionStop - offset && rightTrigger > 0)
                            {
                                signalCursor.X = signalCursor.X + rightTrigger / 10.0;
                            }

                            else if (timeline.CurrentPlayPosition > timeline.SelectionStop - offset && rightTrigger > 0)
                                {
                                    signalCursor.X = signalCursor.X + rightTrigger / 10.0;
                                    double factor = (Time.SelectionStop - Time.SelectionStart) / Time.TotalDuration;
                                    control.timeLineControl.rangeSlider.MoveAndUpdate(true, factor);
                            }

                            if (rightTrigger > 0 || leftTrigger > 0)
                            {

                                timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                                MainHandler.Time.CurrentPlayPosition = timeline.CurrentPlayPosition;
                                mediaList.Move(Time.TimeFromPixel(signalCursor.X));
                            }
                          
                        }));
                        




                    if (gamepad.Buttons != gamepad_last.Buttons)
                    {
                        Console.WriteLine(gamepad.Buttons);

                        if(gamepad.Buttons == GamepadButtonFlags.Y)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                                ToggleLiveMode();
                            }));           
                        }

                        else if (gamepad.Buttons == GamepadButtonFlags.A)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                                TogglePlay();
                            }));
                        }

                            else if (gamepad.Buttons == GamepadButtonFlags.X)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                                   if(AnnoTierStatic.Selected != null)  AnnoTierStatic.Selected.ClearButtonPressed = true;
                                }));
                            }


                            //else if (gamepad.Buttons == GamepadButtonFlags.B)
                            //{
                            //    System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                            //        ToggleMouseMode();
                            //    }));
                            //}

                            else if (gamepad.Buttons == GamepadButtonFlags.DPadUp)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                                   
                                    SetContinuousLevelUp();
                                }));
                            }

                            else if (gamepad.Buttons == GamepadButtonFlags.DPadDown)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                                    SetContinuousLevelDown();
                                }));
                            }

                            else if (gamepad.Buttons == GamepadButtonFlags.None)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke((Action)(() => {
                                    if (AnnoTierStatic.Selected != null)  AnnoTierStatic.Selected.ClearButtonPressed = false;
                                }));
                            }
                        }

                    }
  

                  //  Console.WriteLine(leftThumb.X + " " + leftThumb.Y + " " + leftTrigger + " " + rightTrigger + "  "  + gamepad.Buttons);
                }
          
                   // Console.WriteLine(state.Gamepad);
                Thread.Sleep(10);
                previousState = state;
            }

            AnnoTierStatic.Selected.ControllerConnected = false;


        }




        public void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!control.annoListControl.editTextBox.IsFocused
                && !control.annoListControl.searchTextBox.IsFocused)
            {
                int level = (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) == true ? 1 : 0) + (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) == true ? 1 : 0) + (e.KeyboardDevice.IsKeyDown(Key.LeftShift) == true ? 1 : 0);
                switch (level)
                {
                    /*All Modifier keys are pressed*/
                    case 3:
                        break;
                    /*Two Modifier keys are pressed*/
                    case 2:
                        break;
                    /*One Modifier keys are pressed*/
                    case 1:
                        if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                        {
                            if (e.KeyboardDevice.IsKeyDown(Key.B))
                            {
                                reloadBackupSelectedAnno();
                                e.Handled = true;
                            }
                            else if (e.KeyboardDevice.IsKeyDown(Key.C))
                            {
                                CopySegment();

                                e.Handled = true;
                            }

                            else if (e.KeyboardDevice.IsKeyDown(Key.L))
                            {
                                CompleteSession();
                                e.Handled = true;
                            }

                            else if (e.KeyboardDevice.IsKeyDown(Key.K))
                            {
                                DeleteRemainingSegments();
                                e.Handled = true;
                            }

                            else if (e.KeyboardDevice.IsKeyDown(Key.V))
                            {
                                PasteSegment();
                                e.Handled = true;
                            }

                            else if (e.KeyboardDevice.IsKeyDown(Key.X))
                            {
                                CutSegment(sender, e);
                                e.Handled = true;
                            }


                            else if (e.KeyboardDevice.IsKeyDown(Key.Z))
                            {
                                Undo();
                                e.Handled = true;
                            }

                            else if (e.KeyboardDevice.IsKeyDown(Key.Y))
                            {
                                Redo();
                                e.Handled = true;
                            }


                            else if (e.KeyboardDevice.IsKeyDown(Key.Left))
                            {
                                MoveSegmentBorder(false);
                                e.Handled = true;
                            }
                            else if (e.KeyboardDevice.IsKeyDown(Key.Right))
                            {
                                MoveSegmentBorder();
                                e.Handled = true;
                            }

                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt))
                        {
                            if (e.KeyboardDevice.IsKeyDown(Key.Down))
                            {
                                CopyLabelToTier();
                                e.Handled = true;
                            }

                            else if (e.KeyboardDevice.IsKeyDown(Key.Up))
                            {
                                CopyLabelToTier(false);
                                e.Handled = true;
                            }
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        {

                        }
                            break;
                    /*No Modifier keys are pressed*/
                    case 0:
                        if (e.KeyboardDevice.IsKeyDown(Key.S))
                        {
                            if (AnnoTierStatic.Label != null)
                            {
                                SplitSegment(sender, e);
                            }

                            else
                            {
                                annoCursor.X = signalCursor.X;
                            }
                            e.Handled = true;
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.T) && e.KeyboardDevice.IsKeyDown(Key.Down))
                        {
                            ChangeTier();
                            e.Handled = true;
                        }
                        if (e.KeyboardDevice.IsKeyDown(Key.T) && e.KeyboardDevice.IsKeyDown(Key.Up))
                        {
                            ChangeTier(false);
                            e.Handled = true;
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.Left))
                        {
                            MoveFrameMedia(false);

                            e.Handled = true;
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.Right))
                        {
                            MoveFrameMedia();
                            e.Handled = true;
                        }

                        else if (e.KeyboardDevice.IsKeyDown(Key.Space))
                        {
                            TogglePlay();
                            e.Handled = true;
                        }

                        else if (AnnoTierStatic.Selected != null)
                        {
                            if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                            {
                                if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
                                { 
                                    SetLabelForSegment(e);                            
                                }
                            }
                            else if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && AnnoTierStatic.isNotLiveAnnoMode)
                            {                                
                                if (e.KeyboardDevice.IsKeyDown(Key.Up))
                                {
                                    SetContinuousLevelUp();
                                    e.Handled = true;
                                }                                
                                else if (e.KeyboardDevice.IsKeyDown(Key.Down))
                                {
                                    SetContinuousLevelDown();
                                    e.Handled = true;
                                }
                                else if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
                                {
                                    SetContinuousLevel(e);
                                }
                            }
                        }

                      
                        break;
                    default:
                        break;
                }
            }

        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.control.annoListControl.editTextBox.IsFocused)
            {

                int level = (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) == true ? 1 : 0) + (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) == true ? 1 : 0) + (e.KeyboardDevice.IsKeyDown(Key.LeftShift) == true ? 1 : 0);

                switch (level)
                {
                    /*All Modifier keys are pressed*/
                    case 3:
                        break;
                    /*Two Modifier keys are pressed*/
                    case 2:
                        if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.LeftAlt))
                        {

                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        {

                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.LeftShift) && e.KeyboardDevice.IsKeyDown(Key.LeftAlt))
                        {
                            if (e.KeyboardDevice.IsKeyDown(Key.Right))
                            {
                                MoveFrameAnnotation();
                                e.Handled = true;
                            }
                            else if (e.KeyboardDevice.IsKeyDown(Key.Left))
                            {
                                MoveFrameAnnotation(false);
                                e.Handled = true;
                           }
                        }
                        break;
                    /*One Modifier keys are pressed*/
                    case 1:
                        if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                        {                            
                            if (e.KeyboardDevice.IsKeyDown(Key.S))
                            {
                                saveSelectedAnno();
                            }

                            else  if (e.KeyboardDevice.IsKeyDown(Key.N))
                                {
                                    addNewAnnotation();
                                }
                                else if (e.KeyboardDevice.IsKeyDown(Key.R))
                            {
                                ReloadAnnotations();
                                e.Handled = true;
                            }
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        {
                            if (e.KeyboardDevice.IsKeyDown(Key.R))
                            {
                                AlignToSampleRate();
                            }
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt))
                        {
                            if (e.KeyboardDevice.IsKeyDown(Key.Right))
                            {
                                MoveFrameAnnotation();
                                e.Handled = true;
                            }
                            else if (e.KeyboardDevice.IsKeyDown(Key.Left))
                            {
                                MoveFrameAnnotation(false);
                                e.Handled = true;
                            }
                        }
                        break;
                    /*No Modifier keys are pressed*/
                    case 0:
                        if (e.KeyboardDevice.IsKeyDown(Key.E))
                        {
                            SelectSegmentEnd();
                            e.Handled = true;
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.L))
                        {
                            ToggleLiveMode();
                            e.Handled = true;
                        }                        
                        else if (e.KeyboardDevice.IsKeyDown(Key.M))
                        {
                            ToggleMouseMode();
                            e.Handled = true;
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.Q))
                        {
                            SelectSegmentStart();
                            e.Handled = true;
                        }
                        
                        else if ((e.KeyboardDevice.IsKeyDown(Key.W)))
                        {
                            CreateOrSelectAnnotation();
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.Back))
                        {
                            RemoveSegment(sender, e);
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.Delete))
                        {
                            RemoveSegment(sender, e);
                        }
                        else if (e.KeyboardDevice.IsKeyDown(Key.F5))
                        {
                            ReloadAnnotations();
                            e.Handled = true;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            isKeyDown = false;
        }

        #region Wrapper
        /*TODO: Check for redundant code within functions with similar functionality*/
        private void TogglePlay()
        {
            if (IsPlaying())
            {
                Stop();
            }
            else
            {
                Play();
            }
        }

        private void SplitSegment(object sender, KeyEventArgs e)
        {
            
                AnnoTierStatic.SplitPressed(sender, e);
            
        }

        private void RemoveSegment(object sender, KeyEventArgs e)
        {

            if(AnnoTier.Selected != null && AnnoTier.Selected.IsDiscreteOrFree)
            {

           
            if (AnnoTierStatic.Label != null)
            {
                AnnoTierStatic.RemoveSegmentPressed(sender, e);
            }


            }

            else if(AnnoTier.Selected != null && AnnoTier.Selected.IsContinuous)
            {
               

                AnnoListItem[] selected = new AnnoListItem[control.annoListControl.annoDataGrid.SelectedItems.Count];
                control.annoListControl.annoDataGrid.SelectedItems.CopyTo(selected, 0);
                control.annoListControl.annoDataGrid.SelectedIndex = -1;
                foreach (AnnoListItem s in selected)
                {
                    s.Score = double.NaN;
                }
                AnnoTier.Selected.TimeRangeChanged(MainHandler.Time);

            }
           


        }

        private void SelectSegmentStart()
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

        private void SelectSegmentEnd()
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

        private void SetLabelForSegment(KeyEventArgs e)
        {
        if (AnnoTierStatic.Selected != null)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.D0) || e.KeyboardDevice.IsKeyDown(Key.NumPad0))
            {
                if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.AnnoList != null)
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
                }
                // e.Handled = true;
            }
            else
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
                // e.Handled = true;
            }
        }
        }

        private void CopyLabelToTier(bool down = true)
        {
            if (down)
            {
                if (AnnoTierStatic.Label != null)
                {
                    AnnoListItem temp = AnnoTierStatic.Label.Item;

                    for (int i = 0; i < annoTiers.Count; i++)
                    {
                        if (annoTiers[i] == AnnoTierStatic.Selected && i + 1 < annoTiers.Count)
                        {
                            AnnoTierStatic.Select(annoTiers[i + 1]);
                            if(AnnoTierStatic.Selected.IsDiscreteOrFree)
                            {
                                AnnoTier.SelectLabel(null);
                                if (!AnnoTierStatic.Selected.AnnoList.Contains(temp)) {
                                    AnnoTierStatic.Selected.NewAnnoCopy(temp.Start, temp.Stop, temp.Label, temp.Color);
                                    break;
                                }
                                
                            }
                           
                        }
                    }
                }
            }
            else
            {
                if (AnnoTierStatic.Label != null)
                {
                    AnnoListItem temp = AnnoTierStatic.Label.Item;

                    for (int i = 0; i < annoTiers.Count; i++)
                    {
                        if (annoTiers[i] == AnnoTierStatic.Selected && i > 0)
                        {
                            AnnoTierStatic.Select(annoTiers[i - 1]);
                            if (AnnoTierStatic.Selected.IsDiscreteOrFree)
                            {
                                AnnoTierStatic.SelectLabel(null);
                                if (!AnnoTierStatic.Selected.AnnoList.Contains(temp))
                                {
                                    AnnoTierStatic.Selected.NewAnnoCopy(temp.Start, temp.Stop, temp.Label, temp.Color);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void MoveSegmentBorder(bool right = true)
        {
            if (right)
            {
                if (AnnoTierStatic.Label != null && AnnoTierStatic.Selected.IsDiscreteOrFree && isKeyDown == false /*&& AnnoTierStatic.Label == null*/)
                {
                    UIElement container = VisualTreeHelper.GetParent(AnnoTierStatic.Label) as UIElement;
                    Point relativeLocation = AnnoTierStatic.Label.TranslatePoint(new Point(0, 0), container);

                    mediaList.Move(Time.TimeFromPixel(relativeLocation.X + AnnoTierStatic.Label.Width));

                    if (Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        annoCursor.X = relativeLocation.X + AnnoTierStatic.Label.Width;
                    }
                    else signalCursor.X = relativeLocation.X + AnnoTierStatic.Label.Width;

                    timeline.CurrentSelectPosition = annoCursor.X;
                    timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                    AnnoTierStatic.Label.select(true);
                    isKeyDown = true;
                }
            }
            else
            {
                if (AnnoTierStatic.Label != null && AnnoTierStatic.Selected.IsDiscreteOrFree && isKeyDown == false /*&& AnnoTierStatic.Label == null*/)
                {
                    UIElement container = VisualTreeHelper.GetParent(AnnoTierStatic.Label) as UIElement;
                    Point relativeLocation = AnnoTierStatic.Label.TranslatePoint(new Point(0, 0), container);

                    mediaList.Move(Time.TimeFromPixel(relativeLocation.X));

                    if (Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        annoCursor.X = relativeLocation.X;
                    }
                    else signalCursor.X = relativeLocation.X;

                    timeline.CurrentSelectPosition = annoCursor.X;
                    timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                    AnnoTierStatic.Label.select(true);
                    isKeyDown = true;
                }
            }
        }


        private void SetContinuousLevel(KeyEventArgs e)
        {
            if (e.Key - Key.D1 < Properties.Settings.Default.ContinuousHotkeysNumber && e.Key - Key.D1 >= 0)
            {
                AnnoTierStatic.Selected.continuousSegmentToPosition(e.Key - Key.D1);
            }
        }

        private void SetContinuousLevelUp()
        {
         
               AnnoTierStatic.Selected.continuousSegmentUpDown(true);
            
        }

        private void SetContinuousLevelDown()
        {

            AnnoTierStatic.Selected.continuousSegmentUpDown(false);

        }


        private void PasteSegment()
        {
 
            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.IsDiscreteOrFree)
            {
                double start = Time.TimeFromPixel(annoCursor.X);
                if (temp_segment != null) AnnoTierStatic.Selected.NewAnnoCopy(start, start + temp_segment.Item.Duration, temp_segment.Item.Label, temp_segment.Item.Color, temp_segment.Item.Confidence);
            }

        }

        private void CutSegment(object sender, KeyEventArgs e)
        {
            if (AnnoTierStatic.Label != null && AnnoTierStatic.Selected.IsDiscreteOrFree)
            {
                temp_segment = AnnoTierStatic.Label;
                AnnoTierStatic.RemoveSegmentPressed(sender, e);
            }
        }

        private void CopySegment()
        {
            if (AnnoTierStatic.Label != null && AnnoTierStatic.Selected.IsDiscreteOrFree)
            {
                temp_segment = AnnoTierStatic.Label;
                AnnoTierStatic.Label.select(false);
            }
        }

        private void MoveFrameAnnotation(bool right = true)
        {
  
            double lowest_sr = Properties.Settings.Default.DefaultDiscreteSampleRate <= 0 ? 25 : Properties.Settings.Default.DefaultDiscreteSampleRate;

            foreach (IMedia im in mediaList)
            {
                if (im.GetSampleRate() < lowest_sr)
                {
                    lowest_sr = im.GetSampleRate();
                }
            }

            double fps = 1.0 / lowest_sr;

            if (right)
            {
                 mediaList.Move(Time.TimeFromPixel(signalCursor.X) + fps);

                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    annoCursor.X = annoCursor.X + Time.PixelFromTime(fps);
                }
                else signalCursor.X = signalCursor.X + Time.PixelFromTime(fps);

                timeline.CurrentSelectPosition = annoCursor.X;
                timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);

                if (AnnoTierStatic.Label != null)
                {
                    double start = annoCursor.X;
                    double end = signalCursor.X;

                    if (Keyboard.IsKeyDown(Key.LeftShift))
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
            }
            else
            {
               
                mediaList.Move(Time.TimeFromPixel(signalCursor.X) - fps);
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    annoCursor.X = annoCursor.X - Time.PixelFromTime(fps);
                }
                else
                {
                    signalCursor.X = signalCursor.X - Time.PixelFromTime(fps);
                }

                timeline.CurrentSelectPosition = annoCursor.X;
                timeline.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);

                double start = annoCursor.X;
                double end = signalCursor.X;

                if (Keyboard.IsKeyDown(Key.LeftShift))
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
            }
        }

        private void MoveFrameMedia(bool right = true)
        {
            if (right)
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
                double pos = Time.PixelFromTime(timeline.CurrentPlayPosition);
                signalCursor.X = pos;

                if (Time.CurrentPlayPosition >= Time.SelectionStop && control.navigator.autoScrollCheckBox.IsChecked == true)
                {
                    double factor = (((timeline.CurrentPlayPosition - timeline.SelectionStart) / (timeline.SelectionStop - timeline.SelectionStart)));
                    control.timeLineControl.rangeSlider.followmedia = true;
                    control.timeLineControl.rangeSlider.MoveAndUpdate(true, factor);

                    if (timeline.SelectionStop - timeline.SelectionStart < 1) timeline.SelectionStart = timeline.SelectionStop - 1;
                    signalCursor.X = 1;
                }
                else if (control.navigator.autoScrollCheckBox.IsChecked == false) control.timeLineControl.rangeSlider.followmedia = false;

                double time = Time.TimeFromPixel(pos);
                updatePositionLabels(time);
                control.annoTierControl.currentTime = time;
            }
            else
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
                double pos = Time.PixelFromTime(timeline.CurrentPlayPosition);
                signalCursor.X = pos;

                if (Time.CurrentPlayPosition < Time.SelectionStart && Time.SelectionStart > 0 && control.navigator.autoScrollCheckBox.IsChecked == true)
                {
                    double factor = (((timeline.SelectionStop - timeline.CurrentPlayPosition) / (timeline.SelectionStop - timeline.SelectionStart)));
                    control.timeLineControl.rangeSlider.followmedia = true;
                    control.timeLineControl.rangeSlider.MoveAndUpdate(false, factor);

                    if (timeline.SelectionStop - timeline.SelectionStart < 1) timeline.SelectionStart = timeline.SelectionStop - 1;
                    signalCursor.X = Time.PixelFromTime(Time.SelectionStop);
                }
                else if (control.navigator.autoScrollCheckBox.IsChecked == false) control.timeLineControl.rangeSlider.followmedia = false;

                double time = Time.TimeFromPixel(pos);
                if (time != 0)
                {
                    updatePositionLabels(time);
                    control.annoTierControl.currentTime = Time.TimeFromPixel(pos);
                }
            }
        }

        private void ToggleLiveMode()
        {
            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.IsContinuous)
            {
                if (control.annoLiveModeCheckBox.IsChecked == true)
                {
                    AnnoTierStatic.Selected.LiveAnnoMode(true);
                    control.annoLiveModeCheckBox.IsChecked = false;
                }
                else
                {
                    control.annoLiveModeCheckBox.IsChecked = true;
                    AnnoTierStatic.Selected.LiveAnnoMode(false);
                }
            }

            else if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.IsDiscreteOrFree)
            {
                AnnoTierStatic.UnselectLabel();
                double pos = Time.PixelFromTime(timeline.CurrentPlayPosition);
                MainHandler.Time.CurrentSelectPosition = pos;
                annoCursor.X = pos;


            }

            isKeyDown = true;

        }

        private void ToggleMouseMode()
        {
            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.IsContinuous)
            {
                if (control.annoLiveModeActivateMouse.IsChecked == true)
                {
                    control.annoLiveModeActivateMouse.IsChecked = false;
                }
                else
                {
                    control.annoLiveModeActivateMouse.IsChecked = true;
                }
            }
            isKeyDown = true;
        }

        private void CreateOrSelectAnnotation()
        {
            if (AnnoTierStatic.Selected != null)
            {

                if (AnnoTierStatic.Selected.IsDiscreteOrFree)
                {


                    if (AnnoTierStatic.Label == null && !Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        AnnoTierStatic.Selected.NewAnnoKey();
                    }
                    else if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        ShowLabelBox();
                    }
                    if (AnnoTierStatic.Label != null) AnnoTierStatic.Label.select(true);
                    isKeyDown = true;
                }
                else
                {
                        ShowLabelBoxContinuous();

                    isKeyDown = true;
                }
            }
        }

        private void ReloadAnnotations()
        {
            AnnoTier track = AnnoTierStatic.Selected;
            if (track != null)
            {
                if (track.AnnoList.Source.HasFile)
                {
                    reloadAnnoTierFromFile(track);
                }
                else
                {
                    ReloadAnnoTierFromDatabase(track, false);
                }
            }
        }

        private void ChangeTier(bool down = true)
        {
            if (down)
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
            }
            else
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
            }
        }

        private void CompleteSession()
        {
            if (AnnoTierStatic.Selected != null)
            {
                saveSelectedAnno(true);
                if (AnnoTierStatic.Selected.CMLCompleteTrainOptions != null
                    && AnnoTierStatic.Selected.CMLCompletePredictOptions != null)
                {


                    //(AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS) ? MainHandler.Time.CurrentPlayPosition :
                    //   MainHandler.Time.CurrentSelectPosition)
                    //AnnoTierStatic.Selected.CMLCompleteTrainOptions.Replace(arguments["password"].ToString(), "*");

                    string[] completetrainoptionstemp = AnnoTierStatic.Selected.CMLCompleteTrainOptions.Split(' ');
                    completetrainoptionstemp[14] = ((AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS) ? MainHandler.Time.CurrentPlayPosition : Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition)).ToString(); ;
                    string infofile = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetRandomFileName();
                    //completetrainoptionstemp[16] = infofile;
                    string completetrainoptions = string.Join(" ", completetrainoptionstemp);


                    string[] completepredictoptionstemp = AnnoTierStatic.Selected.CMLCompletePredictOptions.Split(' ');
                    completepredictoptionstemp[16] = ((AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS) ? MainHandler.Time.CurrentPlayPosition : Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition)).ToString(); ;
                    string completepredictoptions = string.Join(" ", completepredictoptionstemp);


                    string[] combinations = new string[1];
                    string database = completetrainoptionstemp[21];
                    string annotator = completetrainoptionstemp[24];
                    string roles = completetrainoptionstemp[22];
                    string scheme =  completetrainoptionstemp[23];
                    string stream = completetrainoptionstemp[25].TrimStart('/', '"').TrimEnd('"', '/'); ;
                    string sessions = completetrainoptionstemp[2];
                    string trainerpath = completetrainoptionstemp[26].TrimStart('/','"').TrimEnd('"', '/');


                    combinations[0] = database + ":" + annotator + ":" + roles + ":" + stream + ":" + sessions;


                   
                    System.IO.File.WriteAllLines(infofile, combinations);



                    //For image/video training tasks we additionally provide the interface with information. make sure the interface deletes these files after reading.
                    if (stream.Contains(".mp4") || stream.Contains(".avi") || stream.Contains(".mov"))
                    {
                        string trainertemplatesessioninfo = Path.GetDirectoryName(trainerpath) + "\\nova_sessions";
                        System.IO.File.WriteAllLines(trainertemplatesessioninfo, combinations);

                     

                        string[] dbinfo = {"ip="+Properties.Settings.Default.DatabaseAddress.Split(':')[0] +";port="+ Properties.Settings.Default.DatabaseAddress.Split(':')[1]+ ";user=" + Properties.Settings.Default.MongoDBUser +
                                            ";pw="+ MainHandler.Decode(Properties.Settings.Default.MongoDBPass) + ";scheme=" +  scheme + ";root=" + Properties.Settings.Default.DatabaseDirectory + ";cooperative=" + "True"  + ";cmlbegintime=" + completetrainoptionstemp[14]};

                         string trainertemplatedbinfo = Path.GetDirectoryName(trainerpath) + "\\nova_db_info";
                         System.IO.File.WriteAllLines(trainertemplatedbinfo, dbinfo);
                    }

  

                    runCMLProcess("cmltrain", completetrainoptions);
                    runCMLProcess("cmltrain", completepredictoptions);

                    ReloadAnnoTierFromDatabase(AnnoTierStatic.Selected, false);

                    //Make sure Windows User name does not contain spaces, otherwise this caused a crash.
                    string tempOptions = Regex.Replace(AnnoTierStatic.Selected.CMLCompleteTrainOptions, @"""[^""]+""", m => m.Value.Replace(' ', '|'));


                    string[] tokens = tempOptions.Split(' ');
                    if (tokens.Length > 1)
                    {
                        string tempTrainerPath = tokens[tokens.Length - 2];
                        tempTrainerPath  = Regex.Replace(tempTrainerPath, @"""[^""]+""", m => m.Value.Replace('|', ' '));


                        tempTrainerPath = tempTrainerPath.Trim();
                        tempTrainerPath = tempTrainerPath.Replace("\"", "");
                        var dir = new DirectoryInfo(Path.GetDirectoryName(tempTrainerPath));
                        if (!Directory.Exists(Properties.Settings.Default.CMLDirectory)) Directory.CreateDirectory(Properties.Settings.Default.CMLTempTrainerPath);
                        foreach (var file in dir.EnumerateFiles(Path.GetFileName(tempTrainerPath) + "*.trainer*"))
                        {
                            string[] split = file.Name.Split('.');
                            string filename = file.Name;
                            if (split.Length == 2 && split[1] == "trainer")
                            {
                                Properties.Settings.Default.CMLTempTrainerName = split[0];
                                Properties.Settings.Default.Save();
                                split[0] = "latestcmlmodel";
                                filename = string.Join(".", split);
                            }
                            

                            if (File.Exists(Properties.Settings.Default.CMLTempTrainerPath + filename))
                            {
                                File.Delete(Properties.Settings.Default.CMLTempTrainerPath + filename);
                            }
                            file.MoveTo(Properties.Settings.Default.CMLTempTrainerPath + filename);
                            //file.Delete();
                        }
                    }
                    
                }
                else
                {
                    databaseCMLCompleteStep();
                }
            }
        }

        public void DeleteRemainingSegments()
        {
            if (AnnoTierStatic.Selected != null)
            {
               


                if(AnnoTier.Selected.IsDiscreteOrFree)
                {
                    double selectedtime = MainHandler.Time.TimeFromPixel(annoCursor.X);
                    List<AnnoTierSegment> SegmentsToRemove = AnnoTierStatic.Selected.segments.FindAll(s => s.Item.Start > selectedtime);
                    foreach (AnnoTierSegment segment in SegmentsToRemove)
                    {
                        AnnoTierStatic.Selected.RemoveSegment(segment);
                    }
                }
                else if (AnnoTier.Selected.IsContinuous)
                {
                    for(int i=0; i< AnnoTierStatic.Selected.AnnoList.Count; i++)
                    {
                        double selectedtime = MainHandler.Time.TimeFromPixel(signalCursor.X);
                        if (AnnoTierStatic.Selected.AnnoList[i].Start > selectedtime)
                        {
                            AnnoTierStatic.Selected.AnnoList[i].Score = double.NaN;
                        }
                    }

                    AnnoTier.Selected.TimeRangeChanged(Time);

                }

                
            }
        }


        

        private void Undo()
        {
            if (AnnoTierStatic.Selected != null)
            {
                AnnoTierStatic.Selected.UnDoObject.Undo(1);
            }
        }

        private void Redo()
        {
            if (AnnoTierStatic.Selected != null)
            {
                AnnoTierStatic.Selected.UnDoObject.Redo(1);
            }
        }

        private void AlignToSampleRate()
        {
            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0 && AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.AnnoList.Scheme.SampleRate != Properties.Settings.Default.DefaultDiscreteSampleRate)
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


        #endregion Wrapper

    }
}