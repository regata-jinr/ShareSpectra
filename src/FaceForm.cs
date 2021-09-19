/***************************************************************************
 *                                                                         *
 *                                                                         *
 * Copyright(c) 2020-2021, REGATA Experiment at FLNP|JINR                  *
 * Author: [Boris Rumyantsev](mailto:bdrum@jinr.ru)                        *
 *                                                                         *
 * The REGATA Experiment team license this file to you under the           *
 * GNU GENERAL PUBLIC LICENSE                                              *
 *                                                                         *
 ***************************************************************************/

using Regata.Core.DataBase;
using Regata.Core.Cloud;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Regata.Desktop.WinForms.Cloud
{

    public partial class FaceForm : Form
    {
        private CancellationTokenSource _cts;

        private List<string> _currentSpectra;
        private Dictionary<string, string> chosenSpectra;
        private List<string> _nonExistedSpectra;

        private Task _fillSpectra;

        public FaceForm()
        {
            InitializeComponent();

            _cts = new CancellationTokenSource();

            _currentSpectra = new List<string>();
            _nonExistedSpectra = new List<string>();
            chosenSpectra = new Dictionary<string, string>();

            ImageList il = new ImageList();
            il.Images.Add(ShareSpectra.Properties.Resources.untick_0_4);
            il.Images.Add(ShareSpectra.Properties.Resources.tick_0_4);
            il.ImageSize = new Size(50, 50);

            listView1.View = View.LargeIcon;
            listView1.LargeImageList = il;

            try
            {

                _fillSpectra = Task.Run(() =>
                {

                    using (var ic = new RegataContext())
                    {
                        _currentSpectra = ic.SharedSpectra.Select(s => s.fileS).ToList();
                    }

                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при загрузке спектров", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                listView1.Clear();
                if (!_fillSpectra.IsCompleted)
                {
                    MessageBox.Show("Подождите, идёт получение данных из облачного хранилища.", "Загрузка информации о спектрах", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

                chosenSpectra = openFileDialog1.FileNames.ToDictionary(s => Path.GetFileNameWithoutExtension(s));

                var _existedSpectra = _currentSpectra.Intersect(chosenSpectra.Keys).ToList();

                foreach (var tf in _existedSpectra)
                {
                    var liv = new ListViewItem();
                    liv.Text = tf;
                    liv.ImageIndex = 1;
                    listView1.Items.Add(liv);
                }

                _nonExistedSpectra = chosenSpectra.Keys.Except(_existedSpectra).ToList();

                foreach (var tf in _nonExistedSpectra)
                {
                    var liv = new ListViewItem();
                    liv.Text = tf;
                    liv.ImageIndex = 0;
                    listView1.Items.Add(liv);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при загрузке спектров", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
       
        private async void button2_Click(object sender, EventArgs e)
        {

            await UploadFiles(chosenSpectra.Keys);

        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await UploadFiles(_nonExistedSpectra);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
        }

        private async Task UploadFiles(IEnumerable<string> spectra)
        {
            try
            {
                foreach (var f in spectra)
                {
                    await SpectraTools.UploadFileToCloudAsync(chosenSpectra[f], _cts.Token);
                    var currentView = listView1.Items.Cast<ListViewItem>().Where(l => l.Text == f).FirstOrDefault();
                    listView1.Items.Remove(currentView);
                    _currentSpectra.Add(f);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Операция была отменена!", "Отмена загрузки спектров", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при загрузке спектров", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
