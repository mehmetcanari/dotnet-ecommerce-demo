using ECommerce.API.DTO.Request.Account;
using ECommerce.API.DTO.Response.Account;
using ECommerce.API.Repositories.Account;

namespace ECommerce.API.Services.Account;
public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AccountService> _logger;
    
    public AccountService(IAccountRepository accountRepository, ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }
    
    public async Task RegisterAccountAsync(AccountRegisterRequestDto createUserRequestDto, string role)
    {
        try
        {
            List<Model.Account> accounts = await _accountRepository.Read();
            if(accounts.Any(a => a.Email == createUserRequestDto.Email)) //Duplicate email check
            {
                _logger.LogError("Email already exists in the system, try another email");
                throw new Exception("Email already exists in the system, try another email");
            }

            Model.Account account = new Model.Account
            {
                FullName = createUserRequestDto.FullName,
                Email = createUserRequestDto.Email,
                Address = createUserRequestDto.Address,
                PhoneNumber = createUserRequestDto.PhoneNumber,
                DateOfBirth = createUserRequestDto.DateOfBirth.ToUniversalTime(),
                UserCreated = DateTime.UtcNow,
                UserUpdated = DateTime.UtcNow,
                Role = role
            };

            await _accountRepository.Create(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adding account: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }
    
    public async Task UpdateAccountAsync(string email, AccountUpdateRequestDto updateRequestUserDto)
    {
        try
        {
            List<Model.Account> accounts = await _accountRepository.Read();
            Model.Account account = accounts.FirstOrDefault(a => a.Email == email) ?? throw new Exception("User not found");
            
            if(accounts.Any(a => a.Email == updateRequestUserDto.Email)) //Duplicate email check
            {
                _logger.LogError("Email already exists in the system, try another email");
                throw new Exception("Email already exists in the system, try another email");
            }
            
            account.FullName = updateRequestUserDto.FullName;
            account.Email = updateRequestUserDto.Email;
            account.Address = updateRequestUserDto.Address;
            account.PhoneNumber = updateRequestUserDto.PhoneNumber;
            account.UserUpdated = DateTime.UtcNow;

            await _accountRepository.Update(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating account: {Message}", ex.Message);
            throw new Exception(ex.Message);
        }
    }

    public async Task<List<AccountResponseDto>> GetAllAccountsAsync()
    {
        try
        {
            List<Model.Account> accounts = await _accountRepository.Read();
            int accountCount = accounts.Count;
            if(accountCount < 1)
            {
                _logger.LogError("No accounts found");
                throw new Exception("No accounts found");
            }

            return accounts.Select(account => new AccountResponseDto
                {
                    AccountId = account.AccountId,
                    FullName = account.FullName,
                    Email = account.Email,
                    Address = account.Address,
                    PhoneNumber = account.PhoneNumber,
                    DateOfBirth = account.DateOfBirth,
                    Role = account.Role
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching accounts: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task<AccountResponseDto> GetAccountWithIdAsync(int id)
    {
        try
        {
            List<Model.Account> accounts = await _accountRepository.Read();
            Model.Account account = accounts.FirstOrDefault(a => a.AccountId == id) ?? throw new Exception("User not found");
            
            AccountResponseDto responseAccount = new AccountResponseDto
            {
                AccountId = account.AccountId,
                FullName = account.FullName,
                Email = account.Email,
                Address = account.Address,
                PhoneNumber = account.PhoneNumber,
                DateOfBirth = account.DateOfBirth,
                Role = account.Role
            };

            return responseAccount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching account: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task DeleteAccountAsync(int id)
    {
        try
        {
            List<Model.Account> accounts = await _accountRepository.Read();
            Model.Account account = accounts.FirstOrDefault(a => a.AccountId == id) ?? throw new Exception("User not found");
            await _accountRepository.Delete(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting account: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task<AccountResponseDto> GetAccountByEmailAsync(string email)
    {
        try
        {
            List<Model.Account> accounts = await _accountRepository.Read();
            Model.Account account = accounts.FirstOrDefault(a => a.Email == email) ?? 
                throw new Exception($"User with email {email} not found");
            
            AccountResponseDto responseAccount = new AccountResponseDto
            {
                AccountId = account.AccountId,
                FullName = account.FullName,
                Email = account.Email,
                Address = account.Address,
                PhoneNumber = account.PhoneNumber,
                DateOfBirth = account.DateOfBirth,
                Role = account.Role
            };

            return responseAccount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching account by email: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }
}