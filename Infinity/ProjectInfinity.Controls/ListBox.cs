using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
namespace ProjectInfinity.Controls
{
  public class ListBox : System.Windows.Controls.ListBox
  {
    bool _firstTime = true;
    public static readonly DependencyProperty ViewModeProperty = DependencyProperty.Register(
                                                                                                  "ViewMode",
                                                                                                  typeof(string),
                                                                                                  typeof(ListBox),
                                                                                                  new FrameworkPropertyMetadata(null));
    /// <summary>
    /// Identifies the <see cref="Command"/> property.
    /// </summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command",
                                                                                            typeof(ICommand),
                                                                                            typeof(ListBox),
                                                                                            new FrameworkPropertyMetadata
                                                                                              (null,
                                                                                               new PropertyChangedCallback
                                                                                                 (CommandPropertyChanged)));

    /// <summary>
    /// Identifies the <see cref="CommandParameter"/> property.
    /// </summary>
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter",
                                                                                                     typeof(object),
                                                                                                     typeof(ListBox),
                                                                                                     new FrameworkPropertyMetadata
                                                                                                       (null));
    /// <summary>
    /// Identifies the <see cref="CommandTarget"/> property.
    /// </summary>
    public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register("CommandTarget",
                                                                                                  typeof(IInputElement),
                                                                                                  typeof(ListBox),
                                                                                                  new FrameworkPropertyMetadata
                                                                                                    (null));



    public string ViewMode
    {
      get { return (string)GetValue(ViewModeProperty); }
      set { SetValue(ViewModeProperty, value); }
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
      FrameworkElement element = Mouse.DirectlyOver as FrameworkElement;
      while (element != null)
      {
        if (element as ListBoxItem != null)
        {
          this.SelectedItem = element;
          //Trace.WriteLine(String.Format("{0} {1}", this.SelectedIndex, this.SelectedItem));
          Keyboard.Focus((System.Windows.Controls.ListBoxItem)element);
          for (int i = 0; i < Items.Count; ++i)
          {
            IInputElement inpElement = ItemContainerGenerator.ContainerFromIndex(i) as IInputElement;
            if (inpElement != null)
            {
              if (inpElement.IsKeyboardFocused)
              {
                this.SelectedIndex = i;
                break;
              }
            }
          }
          e.Handled = true;
          return;
        }
        element = element.TemplatedParent as FrameworkElement;
      }
      base.OnMouseMove(e);
    }
    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
      if ((e.Source as ListBox) == null) return;
      if (e.Key == System.Windows.Input.Key.Enter)
      {
        ListBox box = e.Source as ListBox;

        //execute the command if there is one
        if (Command != null)
        {
          RoutedCommand routedCommand = Command as RoutedCommand;

          if (routedCommand != null)
          {
            routedCommand.Execute(CommandParameter, CommandTarget);
          }
          else
          {
            Command.Execute(CommandParameter);
          }
        }
        e.Handled = true;
        return;
      }
      base.OnKeyDown(e);
    }
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {

      //execute the command if there is one
      if (Command != null)
      {
        RoutedCommand routedCommand = Command as RoutedCommand;

        if (routedCommand != null)
        {
          routedCommand.Execute(CommandParameter, CommandTarget);
        }
        else
        {
          Command.Execute(CommandParameter);
        }
      }
      e.Handled = true;
      return;
    }


    protected override void OnInitialized(EventArgs e)
    {
      ItemContainerGenerator.StatusChanged += new EventHandler(ItemContainerGenerator_StatusChanged);
    }

    void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
    {
      if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
      {
        if (_firstTime == true)
        {
          _firstTime = false;
          if (SelectedIndex >= 0)
          {
            DependencyObject obj = ItemContainerGenerator.ContainerFromIndex(SelectedIndex);
            Keyboard.Focus((ListBoxItem)obj);
          }
        }
      }
    }

    /// <summary>
    /// Gets or sets the <see cref="ICommand"/> to execute whenever an item is activated.
    /// </summary>
    public ICommand Command
    {
      get { return GetValue(CommandProperty) as ICommand; }
      set { SetValue(CommandProperty, value); }
    }

    /// <summary>
    /// Gets or sets the parameter to be passed to the executed <see cref="Command"/>.
    /// </summary>
    public object CommandParameter
    {
      get { return GetValue(CommandParameterProperty); }
      set { SetValue(CommandParameterProperty, value); }
    }

    /// <summary>
    /// Gets or sets the element on which to raise the specified <see cref="Command"/>.
    /// </summary>
    public IInputElement CommandTarget
    {
      get { return GetValue(CommandTargetProperty) as IInputElement; }
      set { SetValue(CommandTargetProperty, value); }
    }

    private void HookUpCommand(ICommand oldCommand, ICommand newCommand)
    {
      if (oldCommand != null)
      {
        RemoveCommand(oldCommand, newCommand);
      }

      AddCommand(oldCommand, newCommand);
    }

    private void RemoveCommand(ICommand oldCommand, ICommand newCommand)
    {
      //there's nothing to do really if CanExecute changes - the listview should still be enabled
      //			oldCommand.CanExecuteChanged -= CanExecuteChanged;
    }

    private void AddCommand(ICommand oldCommand, ICommand newCommand)
    {
      if (newCommand != null)
      {
        //				newCommand.CanExecuteChanged += CanExecuteChanged;
      }
    }

    private static void CommandPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
      (dependencyObject as ListBox).HookUpCommand(e.OldValue as ICommand, e.NewValue as ICommand);
    }
  }
}
