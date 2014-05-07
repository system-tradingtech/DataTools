﻿/*
  Copyright (C) 2005-2014 Govert van Drimmelen

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.


  Govert van Drimmelen
  govert@icon.co.za
*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExcelDna.ComInterop.ComRegistration;

namespace ExcelDna.Integration.CustomUI
{
    public static class CustomTaskPaneFactory
    {
        private static ExcelCustomTaskPaneAddIn _addin;

        // We keep a list of CustomTaksPanes, so that we can clean up when the add-in is removed or reopened.
        // But we don't want to artificially extend the lifetime of CTPs if the add-in is not keeping a reference.
        // So we use WeakReferences to not interfere with lifetime, but have a chance to clean up for live ones.
        private static readonly List<WeakReference> _customTaskPanes = new List<WeakReference>();

        public static CustomTaskPane CreateCustomTaskPane(Type userControlType, string title) 
        {
            return CreateCustomTaskPane(userControlType, title, Type.Missing);
        }
        
        public static CustomTaskPane CreateCustomTaskPane(Type userControlType, string title, object parent)
        {
            //if (!typeof(System.Windows.Forms.UserControl).IsAssignableFrom(userControlType))
            //{
            //    throw new ArgumentException("userControlType for Custom Task Pane must be derive from type System.Windows.Forms.UserControl");
            //}

            // I could use the ProgId and ClsId of the UserControl type here.
            // But then the registration has to be persistent or coordinated, which I dislike.
            // It's already a problem for the RTD servers.
            // Users that want persistent and consistent names, can sort out registration themselves, or use the ExcelComClass support.
            // Then the CTP is created through the CreateCustomTaskPane("My.ProgId",...) overloads.

            // So when passed the type, I always synthesize a progid.
            // We pick a new Guid as ClassId for this add-in...
            Guid clsId = Guid.NewGuid();
            // ...and make the ProgId from this Guid - max 39 chars.
            string progId = "CtpSrv." + clsId.ToString("N");

            // Instantiate and then register UserControl
            // For some reason, there is a problem loading the user control when running under an elevated UAC token.
            // So we use the form of the registration which tries to first register for the machine, 
            // then fall back to the user hive if that fails.

            try
            {
                object userControl = Activator.CreateInstance(userControlType);
                using (new SingletonClassFactoryRegistration(userControl, clsId))
                using (new ProgIdUacRegistration(progId, clsId))
                using (new ClsIdUacRegistration(clsId, progId))
                {
                    return CreateCustomTaskPane(progId, title, parent);
                }
            }
            catch (UnauthorizedAccessException secex)
            {
                Logging.LogDisplay.WriteLine("The CTP Helper required by add-in {0} could not be registered.\r\nThis may be due to restricted permissions on the user's HKCU\\Software\\Classes key.\r\nError message: {1}", 
                    DnaLibrary.CurrentLibrary.Name, secex.Message);
                return null;
            }
        }

        // UserControl as already registered. Just create via factory and add-in.
        public static CustomTaskPane CreateCustomTaskPane(string controlProgId, string title)
        {
            return CreateCustomTaskPane(controlProgId, title, Type.Missing);
        }

        public static CustomTaskPane CreateCustomTaskPane(string controlProgId, string title, object parent)
        {
            ICTPFactory factory = GetCTPFactory();
            CustomTaskPane newCTP = factory.CreateCTP(controlProgId, title, parent);
            _customTaskPanes.Add(new WeakReference(newCTP));   // TODO: Only removed when add-in is unloaded...???
            return newCTP;
        }

        private static ICTPFactory GetCTPFactory()
        {
            if (_addin == null)
            {
                // Register and create addin
                _addin = new ExcelCustomTaskPaneAddIn { DnaLibrary = DnaLibrary.CurrentLibrary };
                ExcelComAddInHelper.LoadComAddIn(_addin);
            }
            return _addin.Factory;
        }

        internal static void UnloadCustomTaskPanes()
        {
            foreach (WeakReference ctpWr in _customTaskPanes)
            {
                CustomTaskPane ctp = ctpWr.Target as CustomTaskPane;
                if (ctp != null)
                {
                    ctp.Delete();
                    Marshal.FinalReleaseComObject(ctp);
                }
            }
            _customTaskPanes.Clear();
        }

        internal static void DetachAddIn()
        {
            _addin = null;
        }
    }

    internal class ExcelCustomTaskPaneAddIn : ExcelComAddIn, ICustomTaskPaneConsumer
    {
        public ICTPFactory Factory;

        // For CustomTaskPane stuff
        // TODO: ActiveX -> WPF without Windows.Forms....?
        public void CTPFactoryAvailable(ICTPFactory CTPFactoryInst)
        {
            Factory = CTPFactoryInst;
        }

        public override void OnDisconnection(Extensibility.ext_DisconnectMode RemoveMode, ref Array custom)
        {
            // Unloading custom task panes prevents the crash on Excel 2010 64-bit.
            CustomTaskPaneFactory.UnloadCustomTaskPanes();
            CustomTaskPaneFactory.DetachAddIn();
            if (Factory != null)
            {
                Marshal.ReleaseComObject(Factory);
                Factory = null;
            }
            base.OnDisconnection(RemoveMode, ref custom);
        }
    }


}

