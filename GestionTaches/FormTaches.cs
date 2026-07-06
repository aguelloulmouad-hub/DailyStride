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

namespace GestionTaches
{
    public partial class FormTaches : Form
    {
        private int idObjectif;
        private int Utilisateurid;
        public FormTaches(int objectifId, int idUtilisateur)
        {
            InitializeComponent();
            Utilisateurid = idUtilisateur;
            idObjectif = objectifId;
            ChargerTaches();
            AjouterColonneSuppression(dgvNonCommence);
            AjouterColonneSuppression(dgvEnCours);
            AjouterColonneSuppression(dgvTermine);
            AjouterColonneSuppression(dgvAbandonne);
            dgvNonCommence.CellClick += dgv_CellClick;
            dgvEnCours.CellClick += dgv_CellClick;
            dgvTermine.CellClick += dgv_CellClick;
            dgvAbandonne.CellClick += dgv_CellClick;
        }

        private void AjouterColonneSuppression(DataGridView dgv)
        {
            if (!dgv.Columns.Contains("btnSupprimer"))
            {
                DataGridViewButtonColumn btnSupprimer = new DataGridViewButtonColumn();
                btnSupprimer.Name = "btnSupprimer";
                btnSupprimer.HeaderText = "Supprimer";
                btnSupprimer.Text = "Supprimer";
                btnSupprimer.UseColumnTextForButtonValue = true;
                dgv.Columns.Add(btnSupprimer);
            }
        }
        private DataTable GetTachesParStatut(string statut, int idUtilisateur, int idObjectif)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection con = new SqlConnection("Server=LAPULGA\\SQLEXPRESS;Database=GestionTachesDB;Trusted_Connection=True;"))
            {
                try
                {
                    con.Open();
                    string query = @"
                SELECT * 
                FROM GetTachesParStatutEtUtilisateur(@Statut, @Id_utilisateur, @Id_objectif)
                ORDER BY Priorite, date_debut, date_limite";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Statut", statut);
                    cmd.Parameters.AddWithValue("@Id_utilisateur", idUtilisateur);
                    cmd.Parameters.AddWithValue("@Id_objectif", idObjectif);  // Ajout de l'ID de l'objectif

                    SqlDataReader reader = cmd.ExecuteReader();
                    dataTable.Load(reader);
                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur : " + ex.Message);
                }
                finally
                {
                    con.Close();
                }
            }
            return dataTable;
        }


        private void ChargerTaches()
        {
            dgvNonCommence.DataSource = GetTachesParStatut("Non commencé", Utilisateurid, idObjectif);
            dgvEnCours.DataSource = GetTachesParStatut("En cours", Utilisateurid, idObjectif);
            dgvTermine.DataSource = GetTachesParStatut("Terminé", Utilisateurid, idObjectif);
            dgvAbandonne.DataSource = GetTachesParStatut("Abandonné", Utilisateurid, idObjectif);
        }

        private void btnModifierTaches_Click(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection("Server=LAPULGA\\SQLEXPRESS;Database=GestionTachesDB;Trusted_Connection=True;"))
            {
                try
                {
                    con.Open();

                    // Parcours des grilles pour détecter les changements de statut
                    VerifierEtMettreAJourTachesAvecNotifications(con, dgvNonCommence);
                    VerifierEtMettreAJourTachesAvecNotifications(con, dgvEnCours);
                    VerifierEtMettreAJourTachesAvecNotifications(con, dgvTermine);
                    VerifierEtMettreAJourTachesAvecNotifications(con, dgvAbandonne);

                    MessageBox.Show("Mise à jour réussie avec notifications !");
                    ChargerTaches(); // Recharge les données pour afficher les mises à jour
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur : " + ex.Message);
                }
            }
        }

        private void VerifierEtMettreAJourTachesAvecNotifications(SqlConnection con, DataGridView dgv)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                // Récupérer les valeurs actuelles
                int idTache = Convert.ToInt32(row.Cells["Id_tache"].Value);
                string nouveauStatut = row.Cells["statut"].Value?.ToString();
                string titre = row.Cells["titre"].Value?.ToString();
                DateTime? dateLimite = row.Cells["date_limite"].Value as DateTime?;

                // Récupérer l'ancien statut de la tâche dans la base de données
                string statutAncien = "";
                using (SqlCommand cmd = new SqlCommand("SELECT statut FROM Tache WHERE Id_tache = @Id_tache", con))
                {
                    cmd.Parameters.AddWithValue("@Id_tache", idTache);
                    statutAncien = cmd.ExecuteScalar()?.ToString();
                }

                // Vérifier si le statut a changé
                if (!string.Equals(statutAncien, nouveauStatut, StringComparison.OrdinalIgnoreCase))
                {
                    // Appeler la procédure pour gérer les notifications
                    using (SqlCommand cmd = new SqlCommand("GererNotificationsTache", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id_tache", idTache);
                        cmd.Parameters.AddWithValue("@Statut", nouveauStatut);
                        cmd.Parameters.AddWithValue("@Titre", titre);
                        cmd.Parameters.AddWithValue("@Date_limite", dateLimite.HasValue ? (object)dateLimite.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Id_utilisateur", Utilisateurid);
                        cmd.Parameters.AddWithValue("@Id_objectif", idObjectif);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Mettre à jour la tâche avec les nouvelles données
                SqlCommand updateCmd = new SqlCommand("UpdateTache", con);
                updateCmd.CommandType = CommandType.StoredProcedure;
                updateCmd.Parameters.AddWithValue("@Id_tache", idTache);
                updateCmd.Parameters.AddWithValue("@Titre", row.Cells["titre"].Value);
                updateCmd.Parameters.AddWithValue("@Description", row.Cells["description"].Value);
                updateCmd.Parameters.AddWithValue("@DateDebut", row.Cells["date_debut"].Value ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@DateLimite", row.Cells["date_limite"].Value ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@Statut", nouveauStatut);
                updateCmd.Parameters.AddWithValue("@Priorite", row.Cells["Priorite"].Value);
                updateCmd.ExecuteNonQuery();
            }
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;

            if (dgv.Columns[e.ColumnIndex].Name == "btnSupprimer" && e.RowIndex >= 0)
            {
                int idTache = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Id_tache"].Value);

                // Demander une confirmation avant la suppression
                if (MessageBox.Show("Voulez-vous vraiment supprimer cette tâche ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    SupprimerTache(idTache);
                    ChargerTaches(); // Recharger les données après suppression
                }
            }
        }

        private void SupprimerTache(int idTache)
        {
            using (SqlConnection con = new SqlConnection("Server=LAPULGA\\SQLEXPRESS;Database=GestionTachesDB;Trusted_Connection=True;"))
            {
                try
                {
                    con.Open();

                    // Appel de la procédure stockée
                    string query = "EXEC SupprimerTache @Id_tache";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Id_tache", idTache);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Tâche supprimée avec succès !");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors de la suppression : " + ex.Message);
                }
            }
        }


        private void btnAjouterTache_Click(object sender, EventArgs e)
        {
            AjouteTache obj = new AjouteTache(idObjectif, Utilisateurid);
            obj.ShowDialog();
        }

        private void BtnActualiser_Click(object sender, EventArgs e)
        {
            using (SqlConnection con = new SqlConnection("Server=LAPULGA\\SQLEXPRESS;Database=GestionTachesDB;Trusted_Connection=True;"))
            {
                try
                {
                    con.Open();

                    // Appeler la procédure pour vérifier les tâches non terminées et ajouter des notifications de rappel
                    using (SqlCommand cmd = new SqlCommand("VerifierEtNotifierTachesNonTerminees", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Id_utilisateur", Utilisateurid);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Mise à jour des tâches et des notifications de rappel effectuée !");
                    ChargerTaches(); // Recharge les données après actualisation
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur : " + ex.Message);
                }
            }
        }

        private void btnRetour_Click(object sender, EventArgs e)
        {
            FormObj obj = new FormObj(Utilisateurid);
            obj.Show();
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FormAccueil obj = new FormAccueil(Utilisateurid);
            obj.Show();
            this.Hide();
        }

        private void gbNonCommence_Enter(object sender, EventArgs e)
        {

        }
    }
}
