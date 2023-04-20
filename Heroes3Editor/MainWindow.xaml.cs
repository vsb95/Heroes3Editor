using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Heroes3Editor.Models;
using Microsoft.Win32;

namespace Heroes3Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Game Game { get; set; }
        public SearchGame SearchGame { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            heroTabs.Visibility = Visibility.Hidden;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            resourcesPanel.Visibility = Visibility.Collapsed;
        }

        private void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog { Filter = "HoMM3 Savegames |*.CGM;*.GM*" };
            if (openDlg.ShowDialog() != true)
            {
                return;
            }

            Game = new Game(openDlg.FileName);
            SearchGame = new SearchGame(game: Game);

            heroTabs.Items.Clear();
            heroTabs.Visibility = Visibility.Hidden;
            heroCboBox.IsEnabled = true;
            heroSearchBtn.IsEnabled = true;

            status.Text = openDlg.FileName;

            Game.Resources.IndexChanged += RedTeam_IndexChanged;
            RedWood.Text = Game.Resources.Wood.ToString();
            RedMercury.Text = Game.Resources.Mercury.ToString();
            RedOre.Text = Game.Resources.Ore.ToString();
            RedSulfur.Text = Game.Resources.Sulfur.ToString();
            RedCrystal.Text = Game.Resources.Crystal.ToString();
            RedGems.Text = Game.Resources.Gems.ToString();
            RedGold.Text = Game.Resources.Gold.ToString();

        }

        private void SaveCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Game != null;
        }

        private void SaveCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Game.Resources.Wood = StringToIntOrDefault(RedWood, Game.Resources.Wood);
            Game.Resources.Mercury = StringToIntOrDefault(RedMercury, Game.Resources.Mercury);
            Game.Resources.Ore = StringToIntOrDefault(RedOre, Game.Resources.Ore);
            Game.Resources.Sulfur = StringToIntOrDefault(RedSulfur, Game.Resources.Sulfur);
            Game.Resources.Crystal = StringToIntOrDefault(RedCrystal, Game.Resources.Crystal);
            Game.Resources.Gems = StringToIntOrDefault(RedGems, Game.Resources.Gems);
            Game.Resources.Gold = StringToIntOrDefault(RedGold, Game.Resources.Gold);


            var saveDlg = new SaveFileDialog { Filter = "HoMM3 Savegames |*.*GM;*.GM*" };
            if (saveDlg.ShowDialog() == true)
            {
                Game.Save(saveDlg.FileName);
                status.Text = saveDlg.FileName;
            }
        }

        private void RedTeam_IndexChanged(object sender, System.EventArgs e)
        {
            RedWood.Text = Game.Resources.Wood.ToString();
            RedMercury.Text = Game.Resources.Mercury.ToString();
            RedOre.Text = Game.Resources.Ore.ToString();
            RedSulfur.Text = Game.Resources.Sulfur.ToString();
            RedCrystal.Text = Game.Resources.Crystal.ToString();
            RedGems.Text = Game.Resources.Gems.ToString();
            RedGold.Text = Game.Resources.Gold.ToString();
            resourcesPanel.Visibility = Visibility.Visible;
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SearchHero(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(heroCboBox.Text)) return;

            var added = Game.SearchHero(heroCboBox.Text);
            if (added)
            {
                var hero = Game.Heroes.Last();
                var heroTab = new TabItem()
                {
                    Header = hero.Name
                };
                heroTab.Content = new HeroPanel()
                {
                    Hero = hero
                };
                heroTabs.Items.Add(heroTab);
                heroTabs.Visibility = Visibility.Visible;
                heroTab.IsSelected = true;
            }
        }

        private void FindResourcesBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(SetWoodTb.Text, out var val) &&
                int.TryParse(SetOreTb.Text, out var ore) && int.TryParse(SetGoldTb.Text, out var gold))
            {
                Game.FindResources(val, ore, gold);
            }
        }

        private int StringToIntOrDefault(TextBox tb, int defaultValue)
        {
            return StringToIntOrDefault(tb.Text, defaultValue);
        }
        private int StringToIntOrDefault(string value, int defaultValue)
        {
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        private void SearchBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(SearchValueTb.Text, out var result))
            {
                int.TryParse(SearchFromTb.Text, out var index);
                SearchResultText.Text = SearchGame.Search(result, index).ToString();
            }
        }

        private void SetValueBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(SetValueTb.Text, out var result))
            {
                SearchGame.SetValue(result);
            }
        }
    }
}
