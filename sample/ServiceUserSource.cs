using System;
using System.Collections.Generic;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using Stampsy.Social;
using SDWebImage;

namespace Sociopath
{
    public class ServiceUserSource : UITableViewSource
    {
        static UIImage _userImage = UIImage.FromBundle ("User");

        List<ServiceUser> _users;

        public static readonly NSString Key = (NSString)"Cell";

        public ServiceUserSource (IEnumerable<ServiceUser> users)
        {
            _users = new List<ServiceUser> (users);
        }

        public override int RowsInSection (UITableView tableview, int section)
        {
            return _users.Count;
        }

        public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell (Key);
            if (cell == null) {
                cell = new UITableViewCell (UITableViewCellStyle.Subtitle, Key);
            }
            var user = _users[indexPath.Row];

            cell.TextLabel.Text = user.Name;
            cell.DetailTextLabel.Text = user.Nickname;

            cell.ImageView.SetImage (NSUrl.FromString (user.ImageUrl), _userImage);

            return cell;
        }

        public void Add (IEnumerable<ServiceUser> users)
        {
            _users.AddRange (users);
        }
    }}

