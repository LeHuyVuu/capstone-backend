using capstone_backend.Business.DTOs.CoupleInvitation;

namespace capstone_backend.Business.Interfaces;

public interface ICoupleInvitationService
{
    Task<(bool Success, string Message, CoupleInvitationResponse? Data)> SendInvitationDirectAsync(int senderMemberId, SendInvitationDirectRequest request);
    Task<(bool Success, string Message, AcceptInvitationResponse? Data)> AcceptInvitationAsync(int invitationId, int currentMemberId);
    Task<(bool Success, string Message)> RejectInvitationAsync(int invitationId, int currentMemberId);
    Task<(bool Success, string Message)> CancelInvitationAsync(int invitationId, int currentMemberId);
    Task<(bool Success, string Message)> BreakupAsync(int currentMemberId);
    Task<List<CoupleInvitationResponse>> GetReceivedInvitationsAsync(int memberId, string? status, int page, int pageSize);
    Task<List<CoupleInvitationResponse>> GetSentInvitationsAsync(int memberId, string? status, int page, int pageSize);
    Task<List<MemberProfileResponse>> SearchMembersAsync(string query, int currentMemberId, int page, int pageSize);
}
