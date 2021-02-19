namespace DreamTravel.Infrastructure.Database.ContextConfigurations
{
    public class UserConfiguration : BaseEntityTypeConfiguration<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Ignore(i => i.DomainEvents);
            builder.Ignore(i => i.MarkedToDeletion);

            builder.Property(p => p.CustomerDiscountGroup).IsRequired();
            builder.Property(p => p.CountryCode).IsRequired();
            builder.Property(p => p.ContractNo).IsRequired();
            builder.Property(p => p.CustomerNo).IsRequired();
            builder.Property(p => p.CurrencyCode);
            builder.Property(p => p.LicenseModel).IsRequired();
            builder.Property(p => p.CcpTenantId);

            builder.Property(p => p.SupportType).HasConversion(new EnumToStringConverter<SupportType>());

            builder.OwnsOne(
                p => p.ContractId,
                o =>
                {
                    o.WithoutPrefix();
                    o.Property(p => p.CspAgreementId).IsRequired();
                    o.Property(p => p.MicrosoftTenantId).IsRequired();
                    o.Property(p => p.PyraTenantId).IsRequired();
                    o.Property(p => p.ContractType).HasConversion(new EnumToStringConverter<ContractType>());
                });

            base.Configure(builder);
        }
    }
}