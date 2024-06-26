﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Common;
using Domain.SuperAccount.Entity;
using Domain.SuperAccount.Port;
using Domain.SuperAccount.UseCase;
using Infra.Configurations;
using Infra.Context;

using Mapster;

using Microsoft.EntityFrameworkCore;

namespace Infra.Account;

public class SuperAccountAdapter : SuperAccountPort {
  private readonly MainDbContext context;
  private readonly IJwtProvider jwtProvider;

  public SuperAccountAdapter(MainDbContext context, IJwtProvider jwtProvider) {
    this.context = context;
    this.jwtProvider = jwtProvider;
  }

  public async Task<(string, long)> Authenticate(SuperAccountAuthenticate accountAuthenticate, CancellationToken cancellationToken) {
    var user = await this.context.SuperAccounts
      .Where(x => x.Email == accountAuthenticate.Email)
      .SingleOrDefaultAsync(cancellationToken);

    AccountNotFoundException.ThrowIfNull(user);

    var generatedPassword = accountAuthenticate.GenerateHash(accountAuthenticate.Password, user.PasswordSalt);

    AccountNotFoundException.ThrowIfFalse(generatedPassword == user.Password);

    var claims = new[]{
      new Claim("sub", user.Id.ToString()),
      new Claim("role", user.GetType().Name),
    };

    var token = this.jwtProvider.Generate(user.Id, claims);
    return (token.Item1, token.Item2);
  }

  public async Task<SuperAccountEntity> CreateAsync(SuperAccountCreate accountCreate, CancellationToken cancellationToken) {
    var accountEntity = accountCreate.Adapt<SuperAccountEntity>();
    if (accountEntity is null) {
      throw new ArgumentNullException();
    }

    await this.context.SuperAccounts.AddAsync(accountEntity);
    var changes = await this.context.SaveChangesAsync(cancellationToken);

    if (changes.Equals(0)) {
      throw new DbUpdateException();
    }

    return accountEntity;
  }

  public bool Delete(Guid id) {
    throw new NotImplementedException();
  }

  public async Task<SuperAccountEntity> Retrieve(SuperAccountRetrieve accountRetrieve, CancellationToken cancellationToken) {
    return await this.context.SuperAccounts.FindAsync(accountRetrieve.Id, cancellationToken);
  }

  public async Task<SuperAccountEntity[]> Retrieve(DataRequest accountRetrieve, CancellationToken cancellationToken) {
    return await this.context.SuperAccounts.Take(accountRetrieve.Size).Skip(accountRetrieve.Size * accountRetrieve.Page).ToArrayAsync(cancellationToken);
  }

  public bool Update(Guid id) {
    throw new NotImplementedException();
  }
}
