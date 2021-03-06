﻿using System;
using System.Collections.Generic;
using System.IO;
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
using CyDu.Ultis;
using CyDu.Windown;

namespace CyDu.Panel
{
    /// <summary>
    /// Interaction logic for MessageImageBox.xaml
    /// </summary>
    public partial class MessageImageBox : UserControl
    {
        public enum Side { User, Other }
        public string Title { get; set; }
        public string ImgBase64 { get; set; }
        public BitmapImage Avatar { get; set; }
        public MessageImageBox(string Title, string content, string arriveTime,long userid, Side side)
        {
            InitializeComponent();
            ImgBase64 = ImageSupportInstance.getInstance().getImageResourceBaseString(long.Parse(content));
            byte[] data =  System.Convert.FromBase64String(ImgBase64);
            MemoryStream memoryStream = new MemoryStream(data);
            var imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.StreamSource = memoryStream;
            imageSource.EndInit();
            ImageMessContent.Source = imageSource;

            this.Title = Title;
            TileMess.Text = Title + "    " + arriveTime;

            Avatar = ImageSupportInstance.getInstance().GetUserImageFromId(userid);
            ImgMess1.Source = Avatar;
            ImgMess2.Source = Avatar;

            if (side == Side.User)
            {
                ImgMess1.Visibility = Visibility.Hidden;
                ImageMessContent.HorizontalAlignment = HorizontalAlignment.Right;
                TileMess.HorizontalAlignment = HorizontalAlignment.Right;
                TileMess.Text = arriveTime;
            }
            else
            {
                ImgMess2.Visibility = Visibility.Hidden;
            }
        }

        private void ImageMessContent_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ImageWindown windown = new ImageWindown(ImgBase64);
            windown.Show();
        }
    }
}
