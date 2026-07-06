using System.Windows.Forms;
using System;
using System.Data.SqlClient;



namespace GestionTaches
{
    public partial class SignUp : Form
    {
        public SignUp()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string prenom = txtprenom.Text.Trim();
            string nom = txtNom.Text.Trim();
            DateTime dateNaissance = datenes.Value;
            string email = txtEmail.Text.Trim();
            string motDePasse = txtmotdepasse1.Text;
            string confirmMotDePasse = confpwd.Text;

            if (string.IsNullOrEmpty(prenom))
            {
                MessageBox.Show("Veuillez entrer votre prénom.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtprenom.Focus();
                return;
            }

            if (string.IsNullOrEmpty(nom))
            {
                MessageBox.Show("Veuillez entrer votre nom.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNom.Focus();
                return;
            }

            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                MessageBox.Show("Veuillez entrer une adresse email valide (doit contenir '@').", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return;
            }

            if (motDePasse.Length < 6)
            {
                MessageBox.Show("Le mot de passe doit contenir au moins 6 caractères.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtmotdepasse1.Focus();
                return;
            }

            if (!motDePasse.Equals(confirmMotDePasse))
            {
                MessageBox.Show("Les mots de passe ne correspondent pas.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                confpwd.Focus();
                return;
            }

            if ((DateTime.Now.Year - dateNaissance.Year) < 16)
            {
                MessageBox.Show("Vous devez avoir au moins 16 ans.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                datenes.Focus();
                return;
            }

            string connectionString = "Server=LAPULGA\\SQLEXPRESS;Database=GestionTachesDB;Integrated Security=True;";
            string query = "INSERT INTO Utilisateur (nom, prenom, date_naissance, email, mot_de_passe) " +
                           "VALUES (@Nom, @Prenom, @DateNaissance, @Email, @MotDePasse)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nom", nom);
                        cmd.Parameters.AddWithValue("@Prenom", prenom);
                        cmd.Parameters.AddWithValue("@DateNaissance", dateNaissance);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@MotDePasse", motDePasse);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Utilisateur enregistré avec succès !");
                            login obj = new login();
                            obj.Show();
                            this.Hide();
                        }
                        else
                        {
                            MessageBox.Show("Erreur lors de l'enregistrement de l'utilisateur.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur de connexion à la base de données: " + ex.Message);
                }
            }
        }
    }
}