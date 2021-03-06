#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Runtime.InteropServices;
using MediaPortal.ExtensionMethods;

namespace MediaPortal.Mixer
{
  public sealed class Mixer : IDisposable
  {
    #region Events

    public event MixerEventHandler LineChanged;
    public event MixerEventHandler ControlChanged;

    #endregion Events

    #region Methods

    public void Close()
    {
      lock (this)
      {
        if (_handle == IntPtr.Zero)
        {
          return;
        }

        MixerNativeMethods.mixerClose(_handle);

        _handle = IntPtr.Zero;
      }
    }

    public void Dispose()
    {
      if (_mixerControlDetailsVolume != null)
      {
        _mixerControlDetailsVolume.SafeDispose();
      }

      if (_mixerControlDetailsMute != null)
      {
        _mixerControlDetailsMute.SafeDispose();
      }
      _mixerEventListener.LineChanged -= new MixerEventHandler(OnLineChanged);
      _mixerEventListener.ControlChanged -= new MixerEventHandler(OnControlChanged);

      Close();
      _mixerEventListener.DestroyHandle();
      _mixerEventListener = null;
    }

    public void Open()
    {
      Open(0, false);
    }

    public void Open(int mixerIndex, bool isDigital)
    {
      lock (this)
      {
        if (isDigital)
        {
          _componentType = MixerComponentType.SourceWave;
        }
        // not enough to change this..

        if (_mixerEventListener == null)
        {
          _mixerEventListener = new MixerEventListener();
          _mixerEventListener.Start();
        }
        _mixerEventListener.LineChanged += new MixerEventHandler(OnLineChanged);
        _mixerEventListener.ControlChanged += new MixerEventHandler(OnControlChanged);

        MixerNativeMethods.MixerControl mc = new MixerNativeMethods.MixerControl();

        mc.Size = 0;
        mc.ControlId = 0;
        mc.ControlType = MixerControlType.Volume;
        mc.fdwControl = 0;
        mc.MultipleItems = 0;
        mc.ShortName = string.Empty;
        mc.Name = string.Empty;
        mc.Minimum = 0;
        mc.Maximum = 0;
        mc.Reserved = 0;

        IntPtr handle = IntPtr.Zero;

        if (
          MixerNativeMethods.mixerOpen(ref handle, mixerIndex, _mixerEventListener.Handle, 0, MixerFlags.CallbackWindow) !=
          MixerError.None)
        {
          throw new InvalidOperationException();
        }

        _handle = handle;

        _mixerControlDetailsVolume = GetControl(_componentType, MixerControlType.Volume);
        _mixerControlDetailsMute = GetControl(_componentType, MixerControlType.Mute);

        _isMuted = (int)GetValue(_componentType, MixerControlType.Mute) == 1;
        _volume = (int)GetValue(_componentType, MixerControlType.Volume);
      }
    }

    private MixerNativeMethods.MixerControlDetails GetControl(MixerComponentType componentType,
                                                              MixerControlType controlType)
    {
      MixerNativeMethods.MixerLine mixerLine = new MixerNativeMethods.MixerLine(componentType);

      if (MixerNativeMethods.mixerGetLineInfoA(_handle, ref mixerLine, MixerLineFlags.ComponentType) != MixerError.None)
      {
        throw new InvalidOperationException("Mixer.GetControl.1");
      }

      using (
        MixerNativeMethods.MixerLineControls mixerLineControls =
          new MixerNativeMethods.MixerLineControls(mixerLine.LineId, controlType))
      {
        if (MixerNativeMethods.mixerGetLineControlsA(_handle, mixerLineControls, MixerLineControlFlags.OneByType) !=
            MixerError.None)
        {
          throw new InvalidOperationException("Mixer.GetControl.2");
        }

        MixerNativeMethods.MixerControl mixerControl =
          (MixerNativeMethods.MixerControl)
          Marshal.PtrToStructure(mixerLineControls.Data, typeof (MixerNativeMethods.MixerControl));

        return new MixerNativeMethods.MixerControlDetails(mixerControl.ControlId);
      }
    }

    private object GetValue(MixerComponentType componentType, MixerControlType controlType)
    {
      MixerNativeMethods.MixerLine mixerLine = new MixerNativeMethods.MixerLine(componentType);

      if (MixerNativeMethods.mixerGetLineInfoA(_handle, ref mixerLine, MixerLineFlags.ComponentType) != MixerError.None)
      {
        throw new InvalidOperationException("Mixer.OpenControl.1");
      }

      using (
        MixerNativeMethods.MixerLineControls mixerLineControls =
          new MixerNativeMethods.MixerLineControls(mixerLine.LineId, controlType))
      {
        MixerNativeMethods.mixerGetLineControlsA(_handle, mixerLineControls, MixerLineControlFlags.OneByType);
        MixerNativeMethods.MixerControl mixerControl =
          (MixerNativeMethods.MixerControl)
          Marshal.PtrToStructure(mixerLineControls.Data, typeof (MixerNativeMethods.MixerControl));

        using (
          MixerNativeMethods.MixerControlDetails mixerControlDetails =
            new MixerNativeMethods.MixerControlDetails(mixerControl.ControlId))
        {
          MixerNativeMethods.mixerGetControlDetailsA(_handle, mixerControlDetails, 0);

          return Marshal.ReadInt32(mixerControlDetails.Data);
        }
      }
    }

    private void SetValue(MixerComponentType componentType, MixerControlType controlType, bool controlValue)
    {
      MixerNativeMethods.MixerLine mixerLine = new MixerNativeMethods.MixerLine(componentType);

      if (MixerNativeMethods.mixerGetLineInfoA(_handle, ref mixerLine, MixerLineFlags.ComponentType) != MixerError.None)
      {
        throw new InvalidOperationException("Mixer.SetValue.1");
      }

      using (
        MixerNativeMethods.MixerLineControls mixerLineControls =
          new MixerNativeMethods.MixerLineControls(mixerLine.LineId, controlType))
      {
        if (MixerNativeMethods.mixerGetLineControlsA(_handle, mixerLineControls, MixerLineControlFlags.OneByType) !=
            MixerError.None)
        {
          throw new InvalidOperationException("Mixer.SetValue.2");
        }

        MixerNativeMethods.MixerControl mixerControl =
          (MixerNativeMethods.MixerControl)
          Marshal.PtrToStructure(mixerLineControls.Data, typeof (MixerNativeMethods.MixerControl));

        using (
          MixerNativeMethods.MixerControlDetails mixerControlDetails =
            new MixerNativeMethods.MixerControlDetails(mixerControl.ControlId))
        {
          Marshal.WriteInt32(mixerControlDetails.Data, controlValue ? 1 : 0);
          MixerNativeMethods.mixerSetControlDetails(_handle, mixerControlDetails, 0);
        }
      }
    }

    private void SetValue(MixerNativeMethods.MixerControlDetails control, bool value)
    {
      if (control == null)
      {
        return;
      }

      Marshal.WriteInt32(control.Data, value ? 1 : 0);
      MixerNativeMethods.mixerSetControlDetails(_handle, control, 0);
    }

    private void SetValue(MixerNativeMethods.MixerControlDetails control, int value)
    {
      if (control == null)
      {
        return;
      }

      Marshal.WriteInt32(control.Data, value);
      MixerNativeMethods.mixerSetControlDetails(_handle, control, 0);
    }

    private void SetValue(MixerComponentType componentType, MixerControlType controlType, int controlValue)
    {
      MixerNativeMethods.MixerLine mixerLine = new MixerNativeMethods.MixerLine(componentType);

      if (MixerNativeMethods.mixerGetLineInfoA(_handle, ref mixerLine, MixerLineFlags.ComponentType) != MixerError.None)
      {
        throw new InvalidOperationException("Mixer.SetValue.1");
      }

      using (
        MixerNativeMethods.MixerLineControls mixerLineControls =
          new MixerNativeMethods.MixerLineControls(mixerLine.LineId, controlType))
      {
        if (MixerNativeMethods.mixerGetLineControlsA(_handle, mixerLineControls, MixerLineControlFlags.OneByType) !=
            MixerError.None)
        {
          throw new InvalidOperationException("Mixer.SetValue.2");
        }

        MixerNativeMethods.MixerControl mixerControl =
          (MixerNativeMethods.MixerControl)
          Marshal.PtrToStructure(mixerLineControls.Data, typeof (MixerNativeMethods.MixerControl));

        using (
          MixerNativeMethods.MixerControlDetails mixerControlDetails =
            new MixerNativeMethods.MixerControlDetails(mixerControl.ControlId))
        {
          Marshal.WriteInt32(mixerControlDetails.Data, controlValue);
          MixerNativeMethods.mixerSetControlDetails(_handle, mixerControlDetails, 0);
        }
      }
    }

    private void OnLineChanged(object sender, MixerEventArgs e)
    {
      if (LineChanged != null)
      {
        LineChanged(sender, e);
      }
    }

    private void OnControlChanged(object sender, MixerEventArgs e)
    {
      _isMuted = (int)GetValue(_componentType, MixerControlType.Mute) == 1;
      _volume = (int)GetValue(_componentType, MixerControlType.Volume);

      if (ControlChanged != null)
      {
        ControlChanged(sender, e);
      }
    }

    #endregion Methods

    #region Properties

    public bool IsMuted
    {
      get { lock (this) return _isMuted; }
      set { lock (this) SetValue(_mixerControlDetailsMute, _isMuted = value); }
    }


    public int Volume
    {
      get { lock (this) return _volume; }
      set
      {
        lock (this)
          SetValue(_mixerControlDetailsVolume,
                   _volume = Math.Max(this.VolumeMinimum, Math.Min(this.VolumeMaximum, value)));
      }
    }

    public int VolumeMaximum
    {
      get { return 65535; }
    }

    public int VolumeMinimum
    {
      get { return 0; }
    }

    #endregion Properties

    #region Fields

    private MixerComponentType _componentType = MixerComponentType.DestinationSpeakers;
    private IntPtr _handle;
    private bool _isMuted;
    private static MixerEventListener _mixerEventListener;
    private int _volume;
    private MixerNativeMethods.MixerControlDetails _mixerControlDetailsVolume;
    private MixerNativeMethods.MixerControlDetails _mixerControlDetailsMute;

    #endregion Fields
  }
}