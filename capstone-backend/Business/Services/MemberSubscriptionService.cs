using AutoMapper;
using AutoMapper.Execution;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.MemberSubscription;
using capstone_backend.Business.DTOs.Momo;
using capstone_backend.Business.DTOs.SubscriptionPackage;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services
{
    public class MemberSubscriptionService : IMemberSubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MemberSubscriptionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> CancelSubscriptionAsync(int userId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var sub = await _unitOfWork.MemberSubscriptionPackages.GetCurrentActiveSubscriptionAsync(member.Id);
            if (sub == null)
                throw new Exception("Không tìm thấy gói đăng ký đang hoạt động");

            sub.Status = MemberSubscriptionPackageStatus.CANCELLED.ToString();
            _unitOfWork.MemberSubscriptionPackages.Update(sub);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<TransactionResponse> CheckPaymentStatusAsync(int userId, string orderId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var orderParts = orderId.Split("_");
            if (orderParts.Length < 3)
                throw new Exception("Order ID không hợp lệ");
            var transactionId = IdEncoder.Decode(orderParts[2]);

            var tx = await _unitOfWork.Transactions.GetByIdAsync((int)transactionId);
            if (tx == null || tx.UserId != userId)
                throw new Exception("Giao dịch không tồn tại hoặc không thuộc về người dùng");

            if (tx.TransType != 3)
                throw new Exception("Giao dịch không phải thanh toán gói member");

            var sub = await _unitOfWork.MemberSubscriptionPackages.GetByIdAsync(tx.DocNo);
            if (sub == null)
                throw new Exception("Không ghi nhận được gói đăng ký của member");

            var metadata = JsonConverterUtil.DeserializeOrDefault<MomoTransactionMetadata>(tx.ExternalRefCode);
            var response = _mapper.Map<TransactionResponse>(tx);
            response.PayUrl = metadata?.PayUrl;
            response.QrCodeUrl = metadata?.QrCodeUrl;
            response.DeepLink = metadata?.DeepLink;
            response.DeeplinkMiniApp = metadata?.DeeplinkMiniApp;

            response.MemberSubscriptionId = tx.DocNo;
            response.StartDate = sub.StartDate;
            response.EndDate = sub.EndDate;
            response.IsActive = sub.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString() && (!sub.EndDate.HasValue || sub.EndDate >= DateTime.UtcNow);

            return response;
        }

        public async Task<PagedResult<SubscriptionPackageDto>> GetAvailablePackagesAsync(int pageNumber, int pageSize)
        {
            var (packages, totalCount) = await _unitOfWork.SubscriptionPackages.GetPagedAsync(
                pageNumber,
                pageSize,
                p => p.IsDeleted == false &&
                p.IsActive == true &&
                p.Type == "MEMBER",
                p => p.OrderBy(sp => sp.Price)
            );

            var response = _mapper.Map<List<SubscriptionPackageDto>>(packages);
            return new PagedResult<SubscriptionPackageDto>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<MemberSubscriptionResponse?> GetCurrentSubscriptionAsync(int userId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var sub = await _unitOfWork.MemberSubscriptionPackages.GetCurrentActiveSubscriptionAsync(member.Id);
            if (sub == null)
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    sub = await EnsureDefaultSubscriptionAsync(userId);
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new Exception("Kích hoạt gói đăng ký mặc định thất bại: " + ex.Message);
                }
            }
                

            var response = _mapper.Map<MemberSubscriptionResponse>(sub);
            return response;
        }

        public async Task<MemberSubscriptionPackage?> EnsureDefaultSubscriptionAsync(int userId)
        {
            var now = DateTime.UtcNow;

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                return null;

            var active = await _unitOfWork.MemberSubscriptionPackages.GetCurrentActiveSubscriptionAsync(member.Id);
            if (active != null)
                return active;

            var defaultPackage = await _unitOfWork.SubscriptionPackages.GetFirstAsync(
                p => p.Type == "MEMBER" &&
                     p.IsDeleted == false &&
                     p.IsActive == true &&
                     p.IsDefault == true
            );

            if (defaultPackage == null)
                return null;

            var defaultMemberSub = await _unitOfWork.MemberSubscriptionPackages.GetFirstAsync(
                s => s.MemberId == member.Id &&
                     s.Package.Type == "MEMBER" &&
                     s.Package.IsDeleted == false &&
                     s.Package.IsActive == true &&
                     s.Package.IsDefault == true,
                s => s.Include(x => x.Package)
            );

            var isNewSubscription = false;

            if (defaultMemberSub == null)
            {
                defaultMemberSub = new MemberSubscriptionPackage
                {
                    MemberId = member.Id,
                    PackageId = defaultPackage.Id,
                    Status = MemberSubscriptionPackageStatus.ACTIVE.ToString(),
                    StartDate = now,
                    EndDate = null,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _unitOfWork.MemberSubscriptionPackages.AddAsync(defaultMemberSub);
                isNewSubscription = true;
            }
            else
            {
                defaultMemberSub.Status = MemberSubscriptionPackageStatus.ACTIVE.ToString();
                defaultMemberSub.StartDate = now;
                defaultMemberSub.EndDate = null;
                defaultMemberSub.UpdatedAt = now;

                _unitOfWork.MemberSubscriptionPackages.Update(defaultMemberSub);
            }

            await _unitOfWork.SaveChangesAsync();

            if (isNewSubscription)
            {
                var newTx = new Transaction
                {
                    UserId = userId,
                    Amount = defaultPackage.Price ?? 0,
                    Currency = "VND",
                    Description = $"Hệ thống tự động kích hoạt gói cho thành viên: {defaultPackage.PackageName}",
                    DocNo = defaultMemberSub.Id,
                    PaymentMethod = "SYSTEM",
                    TransType = 3, // MEMBER_SUBSCRIPTION
                    Status = TransactionStatus.SUCCESS.ToString(),
                    ExternalRefCode = null
                };

                await _unitOfWork.Transactions.AddAsync(newTx);
                await _unitOfWork.SaveChangesAsync();
            }

            return defaultMemberSub;
        }

        public async Task<PagedResult<TransactionResponse>> GetTransactionHistoryAsync(int userId, int pageNumber, int pageSize)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var (transactions, totalCount) = await _unitOfWork.Transactions.GetPagedAsync(
                pageNumber,
                pageSize,
                tx => tx.UserId == userId && tx.TransType == 3,
                q => q.OrderByDescending(tx => tx.CreatedAt).ThenByDescending(tx => tx.Id)
            );

            var transactionList = transactions.ToList();
            var subscriptionIds = transactionList
                .Select(t => t.DocNo)
                .Distinct()
                .ToList();

            var subscriptionById = new Dictionary<int, MemberSubscriptionPackage>();
            if (subscriptionIds.Count > 0)
            {
                var subscriptions = await _unitOfWork.MemberSubscriptionPackages.GetAsync(s => subscriptionIds.Contains(s.Id));
                subscriptionById = subscriptions.ToDictionary(s => s.Id);
            }

            var responseItems = new List<TransactionResponse>(transactionList.Count);
            foreach (var tx in transactionList)
            {
                var metadata = JsonConverterUtil.DeserializeOrDefault<MomoTransactionMetadata>(tx.ExternalRefCode);

                var item = _mapper.Map<TransactionResponse>(tx);
                item.TransType = "MEMBER_SUBSCRIPTION";

                item.PayUrl = metadata?.PayUrl;
                item.QrCodeUrl = metadata?.QrCodeUrl;
                item.DeepLink = metadata?.DeepLink;
                item.DeeplinkMiniApp = metadata?.DeeplinkMiniApp;

                if (subscriptionById.TryGetValue(tx.DocNo, out var sub))
                {
                    item.MemberSubscriptionId = sub.Id;
                    item.StartDate = sub.StartDate;
                    item.EndDate = sub.EndDate;
                    item.IsActive = sub.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString() && (!sub.EndDate.HasValue || sub.EndDate >= DateTime.UtcNow);
                }
                else
                {
                    item.MemberSubscriptionId = tx.DocNo;
                }

                responseItems.Add(item);
            }

            return new PagedResult<TransactionResponse>
            {
                Items = responseItems,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
