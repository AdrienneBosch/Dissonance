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
	/// Interaction logic for StandardLable.xaml
	/// </summary>
	public partial class StandardLable : UserControl
	{
		public StandardLable ( )
		{
			InitializeComponent ( );
		}

		public static readonly DependencyProperty StandardTextProperty =
			DependencyProperty.Register("StandardText", typeof(string), typeof(StandardLable), new PropertyMetadata("Default Standard"));

		public string StandardText
		{
			get { return ( string ) GetValue ( StandardTextProperty ); }
			set { SetValue ( StandardTextProperty, value ); }
		}
	}
}
