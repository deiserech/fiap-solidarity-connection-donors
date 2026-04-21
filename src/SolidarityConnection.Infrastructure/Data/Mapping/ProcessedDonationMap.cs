using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SolidarityConnection.Domain.Entities;

namespace SolidarityConnection.Infrastructure.Data.Mapping
{
    public class ProcessedDonationMap : IEntityTypeConfiguration<ProcessedDonation>
    {
        public void Configure(EntityTypeBuilder<ProcessedDonation> builder)
        {
            builder.ToTable("ProcessedDonations");

            builder.HasKey(p => p.DonationId);

            builder.Property(p => p.CampaignId)
                .IsRequired();

            builder.Property(p => p.DonationAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(p => p.ProcessedAt)
                .IsRequired();

            builder.HasIndex(p => p.CampaignId);
        }
    }
}