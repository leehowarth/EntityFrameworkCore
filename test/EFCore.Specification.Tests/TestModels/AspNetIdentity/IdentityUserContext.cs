// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Microsoft.EntityFrameworkCore.TestModels.AspNetIdentity
{
    public abstract class IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken> : DbContext
        where TUser : IdentityUser<TKey>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TUserLogin : IdentityUserLogin<TKey>
        where TUserToken : IdentityUserToken<TKey>
    {
        protected IdentityUserContext(DbContextOptions options)
            : base(options)
        {
        }

        protected IdentityUserContext()
        {
        }

        public virtual DbSet<TUser> Users { get; set; }
        public virtual DbSet<TUserClaim> UserClaims { get; set; }
        public virtual DbSet<TUserLogin> UserLogins { get; set; }
        public virtual DbSet<TUserToken> UserTokens { get; set; }

        private class PersonalDataConverter : ValueConverter<string, string>
        {
            public PersonalDataConverter(IPersonalDataProtector protector)
                : base(
                    s => protector.Protect(s),
                    s => protector.Unprotect(s))
            {
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var maxKeyLength = 0;
            var encryptPersonalData = false;
            PersonalDataConverter converter = null;

            builder.Entity<TUser>(
                b =>
                {
                    b.HasKey(u => u.Id);
                    b.HasIndex(u => u.NormalizedUserName).IsUnique();
                    b.HasIndex(u => u.NormalizedEmail);
                    b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

                    b.Property(u => u.UserName).HasMaxLength(256);
                    b.Property(u => u.NormalizedUserName).HasMaxLength(256);
                    b.Property(u => u.Email).HasMaxLength(256);
                    b.Property(u => u.NormalizedEmail).HasMaxLength(256);

                    if (encryptPersonalData)
                    {
                        converter = new PersonalDataConverter(this.GetService<IPersonalDataProtector>());
                        var personalDataProps = typeof(TUser).GetProperties().Where(
                            prop => Attribute.IsDefined(prop, typeof(ProtectedPersonalDataAttribute)));
                        foreach (var p in personalDataProps)
                        {
                            if (p.PropertyType != typeof(string))
                            {
                                throw new InvalidOperationException("Resources.CanOnlyProtectStrings");
                            }

                            b.Property(typeof(string), p.Name).HasConversion(converter);
                        }
                    }

                    b.HasMany<TUserClaim>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
                    b.HasMany<TUserLogin>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
                    b.HasMany<TUserToken>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
                });

            builder.Entity<TUserClaim>(
                b =>
                {
                    b.HasKey(uc => uc.Id);
                });

            builder.Entity<TUserLogin>(
                b =>
                {
                    b.HasKey(
                        l => new
                        {
                            l.LoginProvider, l.ProviderKey
                        });

                    if (maxKeyLength > 0)
                    {
                        b.Property(l => l.LoginProvider).HasMaxLength(maxKeyLength);
                        b.Property(l => l.ProviderKey).HasMaxLength(maxKeyLength);
                    }
                });

            builder.Entity<TUserToken>(
                b =>
                {
                    b.HasKey(
                        t => new
                        {
                            t.UserId, t.LoginProvider, t.Name
                        });

                    if (maxKeyLength > 0)
                    {
                        b.Property(t => t.LoginProvider).HasMaxLength(maxKeyLength);
                        b.Property(t => t.Name).HasMaxLength(maxKeyLength);
                    }

                    if (encryptPersonalData)
                    {
                        var tokenProps = typeof(TUserToken).GetProperties().Where(
                            prop => Attribute.IsDefined(prop, typeof(ProtectedPersonalDataAttribute)));
                        foreach (var p in tokenProps)
                        {
                            if (p.PropertyType != typeof(string))
                            {
                                throw new InvalidOperationException("Resources.CanOnlyProtectStrings");
                            }

                            b.Property(typeof(string), p.Name).HasConversion(converter);
                        }
                    }
                });
        }
    }
}
