using CustomerService.Application.Abstractions.Persistence;using CustomerService.Domain.Entities;using CustomerService.Infrastructure.Data.Persistence.Context;using Microsoft.EntityFrameworkCore;
namespace CustomerService.Infrastructure.Data.Commands;
public sealed class RegistrationRepository(ApplicationDbContext db):IRegistrationRepository
{
 public Task<RegistrationApplication?> GetAsync(Guid id,CancellationToken ct)=>db.RegistrationApplications.SingleOrDefaultAsync(x=>x.Id==id,ct);
 public async Task AddAsync(RegistrationApplication x,CancellationToken ct)=>await db.RegistrationApplications.AddAsync(x,ct);
 public Task<bool> HasActiveDuplicateAsync(string email,string mobile,string? nationalId,CancellationToken ct){var ne=email.Trim().ToUpperInvariant();var nm=new string(mobile.Where(char.IsDigit).ToArray());return db.RegistrationApplications.AnyAsync(x=>(x.NormalizedEmail==ne||x.NormalizedMobileNumber==nm||(nationalId!=null&&x.NationalId==nationalId))&&x.Status!=RegistrationStatus.Cancelled&&x.Status!=RegistrationStatus.Expired,ct);}
 public Task<OtpChallenge?> GetLatestOtpAsync(Guid id,OtpChannel c,CancellationToken ct)=>db.OtpChallenges.Where(x=>x.RegistrationId==id&&x.Channel==c).OrderByDescending(x=>x.CreatedUtc).FirstOrDefaultAsync(ct);
 public async Task AddOtpAsync(OtpChallenge x,CancellationToken ct)=>await db.OtpChallenges.AddAsync(x,ct);
 public async Task InvalidateActiveOtpsAsync(Guid id,OtpChannel c,CancellationToken ct){var rows=await db.OtpChallenges.Where(x=>x.RegistrationId==id&&x.Channel==c&&x.VerifiedUtc==null&&x.InvalidatedUtc==null).ToListAsync(ct);foreach(var x in rows)x.Invalidate();}
 public Task<int> CountOtpsSinceAsync(Guid id,OtpChannel c,DateTime since,CancellationToken ct)=>db.OtpChallenges.CountAsync(x=>x.RegistrationId==id&&x.Channel==c&&x.CreatedUtc>=since,ct);
 public Task<NotificationTemplate?> GetTemplateAsync(string code,NotificationChannel c,CancellationToken ct)=>db.NotificationTemplates.Where(x=>x.Code==code&&x.Channel==c&&x.IsActive).OrderByDescending(x=>x.Version).FirstOrDefaultAsync(ct);
 public async Task AddDeliveryAsync(NotificationDelivery x,CancellationToken ct)=>await db.NotificationDeliveries.AddAsync(x,ct);
 public async Task<IReadOnlyList<NotificationDelivery>> GetDeliveriesAsync(Guid id,CancellationToken ct)=>await db.NotificationDeliveries.Where(x=>x.RegistrationId==id).OrderByDescending(x=>x.CreatedUtc).ToListAsync(ct);
 public async Task AddVerificationAttemptAsync(OtpVerificationAttempt x,CancellationToken ct)=>await db.OtpVerificationAttempts.AddAsync(x,ct);
 public async Task<IReadOnlyList<TermDocument>> GetActiveTermsAsync(CancellationToken ct){var now=DateTime.UtcNow;return await db.TermDocuments.Where(x=>x.IsActive&&x.EffectiveFromUtc<=now&&(x.EffectiveToUtc==null||x.EffectiveToUtc>now)).OrderBy(x=>x.Code).ToListAsync(ct);}
 public Task<TermDocument?> GetTermAsync(Guid id,CancellationToken ct)=>db.TermDocuments.SingleOrDefaultAsync(x=>x.Id==id&&x.IsActive,ct);
 public async Task<bool> HasAcceptedRequiredTermsAsync(Guid id,CancellationToken ct){var now=DateTime.UtcNow;var required=await db.TermDocuments.Where(x=>x.IsRequired&&x.IsActive&&x.EffectiveFromUtc<=now&&(x.EffectiveToUtc==null||x.EffectiveToUtc>now)).Select(x=>new{x.Id,x.Version}).ToListAsync(ct);var accepted=await db.RegistrationConsents.Where(x=>x.RegistrationId==id&&x.Accepted).Select(x=>new{x.TermDocumentId,x.TermVersion}).ToListAsync(ct);return required.All(r=>accepted.Any(a=>a.TermDocumentId==r.Id&&a.TermVersion==r.Version));}
 public async Task AddConsentAsync(RegistrationConsent x,CancellationToken ct){if(!await db.RegistrationConsents.AnyAsync(c=>c.RegistrationId==x.RegistrationId&&c.TermDocumentId==x.TermDocumentId&&c.TermVersion==x.TermVersion,ct))await db.RegistrationConsents.AddAsync(x,ct);}
 public async Task AddStepAsync(Guid id,RegistrationStep step,string status,CancellationToken ct)=>await db.RegistrationStepHistories.AddAsync(new(){Id=Guid.NewGuid(),RegistrationId=id,Step=step,Status=status,OccurredUtc=DateTime.UtcNow},ct);
 public Task<CustomerAccount?> GetCustomerByLegacyIdAsync(string legacyId,CancellationToken ct)=>db.CustomerAccounts.SingleOrDefaultAsync(x=>x.LegacyCustomerId==legacyId,ct);
 public async Task AddCustomerAsync(CustomerAccount x,CancellationToken ct){if(!await db.CustomerAccounts.AnyAsync(c=>c.RegistrationId==x.RegistrationId,ct))await db.CustomerAccounts.AddAsync(x,ct);}
 public Task<int> SaveChangesAsync(CancellationToken ct)=>db.SaveChangesAsync(ct);
}
