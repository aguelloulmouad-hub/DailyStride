using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace GestionTaches
{
    public partial class FormStats : Form
    {
        private int Id_utilisateur;

        public FormStats(int id_utilisateur)
        {
            Id_utilisateur = id_utilisateur;
            InitializeComponent();
            ActualiserStatistiques();
        }

        private void ActualiserStatistiques()
        {
            string connectionString = "Server=LAPULGA\\SQLEXPRESS;Database=GestionTachesDB;Trusted_Connection=True;";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                try
                {
                    con.Open();

                    // 1. Mise à jour des labels
                    lblTotalObjectifs.Text = $"{ObtenirTotal(con, "SELECT COUNT(*) FROM Objectif WHERE Id_utilisateur = @Id_utilisateur")}";
                    lblTotalTâches.Text = $"{ObtenirTotal(con, "SELECT COUNT(*) FROM Tache WHERE Id_utilisateur = @Id_utilisateur")}";
                    lblPourcentageObjectifs.Text = $"{ObtenirPourcentage(con, "SELECT COUNT(*) FROM Objectif WHERE Id_utilisateur = @Id_utilisateur AND statut = 'Terminé'", "SELECT COUNT(*) FROM Objectif WHERE Id_utilisateur = @Id_utilisateur"):F2}%";
                    lblPourcentageTâches.Text = $"{ObtenirPourcentage(con, "SELECT COUNT(*) FROM Tache WHERE Id_utilisateur = @Id_utilisateur AND statut = 'Terminé'", "SELECT COUNT(*) FROM Tache WHERE Id_utilisateur = @Id_utilisateur"):F2}%";

                    // 2. Mise à jour du graphique
                    MettreAJourChart(con);
                    MettreAJourChart2(con);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la récupération des statistiques : {ex.Message}");
                }
            }
        }

        private int ObtenirTotal(SqlConnection con, string query)
        {
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Id_utilisateur", Id_utilisateur);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private double ObtenirPourcentage(SqlConnection con, string queryNumerateur, string queryDenominateur)
        {
            int numerateur = ObtenirTotal(con, queryNumerateur);
            int denominateur = ObtenirTotal(con, queryDenominateur);
            return denominateur == 0 ? 0 : (double)numerateur / denominateur * 100;
        }

        private void MettreAJourChart(SqlConnection con)
        {
            using (SqlCommand cmd = new SqlCommand("SELECT statut, COUNT(*) AS Total FROM Tache WHERE Id_utilisateur = @Id_utilisateur GROUP BY statut", con))
            {
                cmd.Parameters.AddWithValue("@Id_utilisateur", Id_utilisateur);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable data = new DataTable();
                adapter.Fill(data);

                chart.Series.Clear();
                Series series = chart.Series.Add("Répartition des Tâches");
                series.ChartType = SeriesChartType.Pie;

                foreach (DataRow row in data.Rows)
                {
                    string statut = row["statut"].ToString();
                    int total = Convert.ToInt32(row["Total"]);
                    series.Points.AddXY(statut, total);
                }
            }
        }

        private void MettreAJourChart2(SqlConnection con)
        {
            using (SqlCommand cmd = new SqlCommand("GetRepartitionObjectifsParStatut", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id_utilisateur", Id_utilisateur);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable data = new DataTable();
                adapter.Fill(data);

                chart2.Series.Clear();
                Series series = chart2.Series.Add("Répartition des Objectifs");
                series.ChartType = SeriesChartType.Pie;

                foreach (DataRow row in data.Rows)
                {
                    string statut = row["statut"].ToString();
                    int total = Convert.ToInt32(row["Total"]);
                    series.Points.AddXY(statut, total);
                }
            }
        }

        private void btnRetour_Click(object sender, EventArgs e)
        {
            FormAccueil obj = new FormAccueil(Id_utilisateur);
            obj.Show();
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FormAccueil obj = new FormAccueil(Id_utilisateur);
            obj.Show();
            this.Hide();
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void FormStats_Load(object sender, EventArgs e)
        {

        }

        private void panel10_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
