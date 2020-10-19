using Microsoft.EntityFrameworkCore;
using System;

namespace Extensions
{
    public class InfoContext : DbContext
    {
        public DbSet<SharingFilesErrors> UnSharedFiles { get; set; }
        public DbSet<SharedSpectra> SharedSpectra { get; set; }

        public const string RegataDBTarget = "MeasurementsTempConnectionString";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var conStr = "";
#if DEBUG
            conStr = @"Server = RUMLAB\REGATALOCAL; Database = NAA_DB_TEST; Trusted_Connection = True";

#else
            if (Environment.MachineName != "NF-105-17") return;

            var cm = AdysTech.CredentialManager.CredentialManager.GetCredentials(RegataDBTarget);

                if (cm == null)
                    throw new ArgumentException("Can't load data base credential. Please add it to the windows credential manager");

                conStr = cm.Password;

#endif
            optionsBuilder.UseSqlServer(conStr);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SharingFilesErrors>()
                .HasKey(c => c.fileS);


            modelBuilder.Entity<SharedSpectra>()
                   .HasKey(s =>  s.fileS);
        }
    }
}
