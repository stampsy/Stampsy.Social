using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using MonoTouch.UIKit;
using Stampsy.Social;

namespace Sociopath
{
    public class ActionSheetChoiceProvider<T> : IChoiceProvider<T>
    {
        private RectangleF _frame;
        private UIView _inView;

        public ActionSheetChoiceProvider (RectangleF frame, UIView inView)
        {
            _frame = frame;
            _inView = inView;
        }

        public Task<T> ChooseAsync (IEnumerable<T> options, Func<T, string> toString)
        {
            var tcs = new TaskCompletionSource<T> ();

            var buttons = options.Select (toString).ToArray ();
            var sheet = new UIActionSheet ("Choose Account", null, null, null, buttons);

            sheet.Dismissed += (sender, e) => {
                if (e.ButtonIndex == -1)
                    tcs.SetCanceled ();
                else
                    tcs.SetResult (options.ElementAt (e.ButtonIndex));
            };

            sheet.ShowFrom (_frame, _inView, true);
            return tcs.Task;
        }
    }
}

