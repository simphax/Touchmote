using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WiiTUIO.Provider;

namespace WiiTUIO
{
    /// <summary>
    /// Interaction logic for LayoutSelectionRow.xaml
    /// </summary>
    public partial class KeymapConnectionRow : UserControl
    {
        private KeymapInput input;
        private KeymapOutConfig config;

        public Action<Adorner> OnDragStart;
        public Action<Adorner> OnDragStop;
        public Action<KeymapInput, KeymapOutConfig> OnConfigChanged;

        private bool fromDefault;

        public KeymapConnectionRow(KeymapInput input, KeymapOutConfig config, bool fromDefault)
        {
            InitializeComponent();
            this.input = input;
            this.config = config;
            this.fromDefault = fromDefault;

            this.connection_input_name.Text = input.Name;
            if (input.Continous)
            {
                this.stickBlup.Visibility = Visibility.Visible;
                this.rAdd.Visibility = Visibility.Collapsed;
            }
            else if (input.Cursor)
            {
                this.cursorBlup.Visibility = Visibility.Visible;
                this.rAdd.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.buttonBlup.Visibility = Visibility.Visible;
            }

            this.connection_input_config_border.Visibility = Visibility.Collapsed;
            this.connection_input_config_closebutton.Visibility = Visibility.Hidden;
            if(this.input.Continous)
            {
                this.deadzone_updown.Value = this.config.Deadzone;
                this.scale_updown.Value = this.config.Scale;
                this.threshold_updown.Value = this.config.Threshold;
            }
            else
            {
                this.connection_input_config_openbutton.Visibility = Visibility.Hidden;
            }
            

            this.SetConfig(config);
        }

        public void SetConfig(KeymapOutConfig config)
        {
            this.config = config;
            //this.connection_output_name.Text = config.Output.Name;
            this.connection_output_stack.Children.Clear();

            for(int i=0; i<config.Stack.Count; i++)
            {
                KeymapOutput output = config.Stack[i];
                KeymapOutputItem item = new KeymapOutputItem(output);
                item.AllowDrop = true;
                item.OnDragStart += item_OnDragStart;
                item.OnDragStop += item_OnDragStop;
                item.Drop += output_Drop;
                item.DragEnter += output_DragEnter;
                item.DragLeave += output_DragLeave;
                item.Tag = i;
                this.connection_output_stack.Children.Add(item);
            }

            if (config.Inherited)
            {
                this.connection_output_stack.Opacity = 0.6;
                this.rAdd.Visibility = Visibility.Hidden;
                this.rClear.Visibility = Visibility.Hidden;
            }
            else
            {
                this.connection_output_stack.Opacity = 1.0;
                this.rAdd.Visibility = Visibility.Visible;
                this.rClear.Visibility = Visibility.Visible;
            }

            /*
            if (config.Inherited)
            {
                Color color = KeymapColors.GetColor(config.Output.Type);
                color.A = 60;
                this.connection_output_border.Background = new SolidColorBrush(color);
                this.rClear.Visibility = Visibility.Hidden;
            }
            else
            {
                this.connection_output_border.Background = new SolidColorBrush(KeymapColors.GetColor(config.Output.Type));
                this.rClear.Visibility = Visibility.Visible;
            }
            */
            /*if (fromDefault)
            {
                this.rClear.Visibility = Visibility.Hidden;
            }*/

            if (OnConfigChanged != null)
            {
                OnConfigChanged(this.input,this.config);
            }
        }

        private void item_OnDragStop(Adorner adorner)
        {
            if (OnDragStop != null)
            {
                OnDragStop(adorner);
            }
        }

        private void item_OnDragStart(Adorner adorner)
        {
            if (OnDragStart != null)
            {
                OnDragStart(adorner);
            }
        }

        private void output_Drop(object sender, DragEventArgs e)
        {
            if (!this.connection_output_stack.Children.Contains(((KeymapOutputItem)e.Data.GetData("KeymapOutputItem"))))
            {
                if (e.Data.GetDataPresent("KeymapOutput"))
                {
                    KeymapOutput newOutput = (KeymapOutput)e.Data.GetData("KeymapOutput");
                    if (this.input.canHandle(newOutput))
                    {
                        if (this.config.Inherited)
                        {
                            this.SetConfig(new KeymapOutConfig(newOutput, false));
                        }
                        else
                        {
                            if (sender is FrameworkElement && (sender as FrameworkElement).Tag is int)
                            {
                                this.config.Stack[(int)(sender as FrameworkElement).Tag] = newOutput;
                                this.config.Inherited = false;
                                this.SetConfig(this.config);
                            }
                        }
                    }
                    if (e.Data.GetDataPresent("KeymapOutputItem"))
                    {
                        ((KeymapOutputItem)e.Data.GetData("KeymapOutputItem")).DropDone();
                    }
                }
                else
                {
                    if (e.Data.GetDataPresent("KeymapOutputItem"))
                    {
                        ((KeymapOutputItem)e.Data.GetData("KeymapOutputItem")).DropDone();
                    }
                }
            }
        }

        private void output_DragLeave(object sender, DragEventArgs e)
        {
            if (!this.connection_output_stack.Children.Contains(((KeymapOutputItem)e.Data.GetData("KeymapOutputItem"))))
            {
                if (e.Data.GetDataPresent("KeymapOutputItem"))
                {
                    ((KeymapOutputItem)e.Data.GetData("KeymapOutputItem")).DropLost();
                }
            }
        }

        private void output_DragEnter(object sender, DragEventArgs e)
        {
            if (!this.connection_output_stack.Children.Contains(((KeymapOutputItem)e.Data.GetData("KeymapOutputItem"))))
            {
                if (e.Data.GetDataPresent("KeymapOutput"))
                {
                    KeymapOutput newOutput = (KeymapOutput)e.Data.GetData("KeymapOutput");
                    if (this.input.canHandle(newOutput))
                    {
                        if (e.Data.GetDataPresent("KeymapOutputItem"))
                        {
                            UIElement target;
                            if (this.config.Inherited)
                            {
                                target = this.connection_output_stack;
                            }
                            else
                            {
                                target = sender as UIElement;
                            }
                            ((KeymapOutputItem)e.Data.GetData("KeymapOutputItem")).DropAccepted(target);
                        }
                    }
                    else
                    {
                        if (e.Data.GetDataPresent("KeymapOutputItem"))
                        {
                            if (!this.connection_output_stack.Children.Contains(((KeymapOutputItem)e.Data.GetData("KeymapOutputItem"))))
                            {
                                ((KeymapOutputItem)e.Data.GetData("KeymapOutputItem")).DropRejected();
                            }
                        }
                    }
                }
            }
        }

        private void rClear_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.config.Stack.Count == 1)
            {
                if (this.fromDefault)
                {
                    this.SetConfig(new KeymapOutConfig(KeymapDatabase.Current.getDisableOutput(), false));
                }
                else
                {
                    Keymap defaultKeymap = KeymapDatabase.Current.getDefaultKeymap();
                    this.config.Stack = defaultKeymap.getConfigFor(0, this.input.Key).Stack;
                    this.config.Inherited = true;
                    this.SetConfig(this.config);
                }
            }
            else if(this.config.Stack.Count > 1)
            {
                this.config.Stack.RemoveAt(this.config.Stack.Count - 1);
                this.SetConfig(this.config);
            }
        }

        private void rAdd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.config.addOutput(KeymapDatabase.Current.getDisableOutput());
            this.SetConfig(this.config);
        }

        private void connection_input_config_openbutton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.connection_input_config_border.Visibility = Visibility.Visible;
            this.connection_input_config_openbutton.Visibility = Visibility.Hidden;
            this.connection_input_config_closebutton.Visibility = Visibility.Visible;
        }

        private void connection_input_config_closebutton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.config.Deadzone = this.deadzone_updown.Value.Value;
            this.config.Scale = this.scale_updown.Value.Value;
            this.config.Threshold = this.threshold_updown.Value.Value;
            this.SetConfig(this.config);
            this.connection_input_config_border.Visibility = Visibility.Collapsed;
            this.connection_input_config_openbutton.Visibility = Visibility.Visible;
            this.connection_input_config_closebutton.Visibility = Visibility.Hidden;
        }

    }
}
