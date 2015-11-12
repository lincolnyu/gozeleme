using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace FileMatcherApp.Extensions
{
    /// <summary>
    ///  Old winform wrapper for WPF
    /// </summary>
    /// <remarks>
    ///  References:
    ///   http://stackoverflow.com/questions/315164/how-to-use-a-folderbrowserdialog-from-a-wpf-application
    /// </remarks>
    public static class WpfWinFormExtensions
    {
        #region Nested classes

        private class OldWindow : IWin32Window
        {
            #region Properties

            private readonly IntPtr _handle;

            #endregion

            #region Constructors

            public OldWindow(IntPtr handle)
            {
                _handle = handle;
            }

            #endregion

            #region Methods

            #region IWin32Window Members

            IntPtr IWin32Window.Handle
            {
                get { return _handle; }
            }

            #endregion

            #endregion
        }

        #endregion

        #region Methods

        public static IWin32Window GetIWin32Window(this Visual visual)
        {
            var source = PresentationSource.FromVisual(visual) as HwndSource;
            if (source == null) return null;
            IWin32Window win = new OldWindow(source.Handle);
            return win;
        }

        #endregion
    }

}
