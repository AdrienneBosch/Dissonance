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

namespace Dissonance.UserControls.Lables
{
	/// <summary>
	/// Interaction logic for HeadingLable.xaml
	/// </summary>
	public partial class HeadingLable : UserControl
	{
		public HeadingLable ( )
		{
			InitializeComponent ( );
		}

		public static readonly DependencyProperty HeadingTextProperty =
			DependencyProperty.Register("HeadingText", typeof(string), typeof(HeadingLable), new PropertyMetadata("Default Heading"));

		public string HeadingText
		{
			get { return ( string ) GetValue ( HeadingTextProperty ); }
			set { SetValue ( HeadingTextProperty, value ); }
		}
	}
}
