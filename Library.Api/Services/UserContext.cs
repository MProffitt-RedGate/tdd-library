namespace Library.Api.Services;

public interface IUserContext
{
    public Member Member { get; set; }
}

public class UserContext(Member member) : IUserContext
{
    public Member Member { get; set; } = member;
}