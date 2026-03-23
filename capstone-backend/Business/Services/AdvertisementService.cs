using capstone_backend.Business.DTOs.Advertisement;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class AdvertisementService : IAdvertisementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdvertisementService> _logger;
    private readonly SepayService _sepayService;
    private readonly RefundService _refundService;
    private readonly WalletPaymentService _walletPaymentService;
    private static int _rotationIndex = 0;
    private static readonly object _lock = new object();
    private static readonly Random _random = new Random();

    public AdvertisementService(
        IUnitOfWork unitOfWork, 
        ILogger<AdvertisementService> logger,
        SepayService sepayService,
        RefundService refundService,
        WalletPaymentService walletPaymentService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _sepayService = sepayService;
        _refundService = refundService;
        _walletPaymentService = walletPaymentService;
    }

    public async Task<List<AdvertisementResponse>> GetRotatingAdvertisementsAsync(string? placementType = null)
    {
        // Lấy quảng cáo active
        var venueLocationAds = await _unitOfWork.Advertisements.GetActiveAdvertisementsAsync();

        _logger.LogInformation("Total active ads before filter: {Count}", venueLocationAds.Count);

        // Filter by placement type nếu có (case-insensitive)
        if (!string.IsNullOrEmpty(placementType))
        {
            var normalizedPlacementType = placementType.Trim().ToUpper();
            venueLocationAds = venueLocationAds
                .Where(vla => !string.IsNullOrEmpty(vla.Advertisement.PlacementType) && 
                             vla.Advertisement.PlacementType.Trim().ToUpper() == normalizedPlacementType)
                .ToList();
            
            _logger.LogInformation("Filtered ads by PlacementType '{PlacementType}': {Count} ads found", 
                placementType, venueLocationAds.Count);
        }

        // Nhóm quảng cáo theo priority score
        var groupedByPriority = venueLocationAds
            .GroupBy(vla => vla.PriorityScore ?? 0)
            .OrderByDescending(g => g.Key)
            .ToList();

        var rotatedAds = new List<AdvertisementResponse>();

        // Xoay vòng quảng cáo trong từng nhóm priority
        foreach (var group in groupedByPriority)
        {
            var adsInGroup = group.ToList();
            
            if (adsInGroup.Count > 0)
            {
                // Xoay vòng: lấy index hiện tại, sau đó tăng lên
                lock (_lock)
                {
                    var startIndex = _rotationIndex % adsInGroup.Count;
                    
                    // Sắp xếp lại danh sách bắt đầu từ startIndex
                    var rotated = adsInGroup.Skip(startIndex)
                        .Concat(adsInGroup.Take(startIndex))
                        .ToList();

                    foreach (var vla in rotated)
                    {
                        rotatedAds.Add(new AdvertisementResponse
                        {
                            Type = "ADVERTISEMENT",
                            AdvertisementId = vla.Advertisement.Id,
                            VenueId = vla.Venue.Id,
                            SpecialEventId = null,
                            BannerUrl = vla.Advertisement.BannerUrl,
                            PlacementType = vla.Advertisement.PlacementType
                        });
                    }

                    // Tăng rotation index cho lần gọi tiếp theo
                    _rotationIndex++;
                }
            }
        }

        // Chỉ thêm special events khi KHÔNG có placementType (không trộn lẫn khi có filter)
        List<AdvertisementResponse> result;
        if (string.IsNullOrEmpty(placementType))
        {
            // Lấy special events active
            var specialEvents = await _unitOfWork.SpecialEvents.GetActiveSpecialEventsAsync();
            
            // Trộn special events vào (priority thấp hơn, đặt ở cuối)
            var specialEventResponses = specialEvents.Select(se => new AdvertisementResponse
            {
                Type = "SPECIAL_EVENT",
                AdvertisementId = null,
                VenueId = null,
                SpecialEventId = se.Id,
                BannerUrl = se.BannerUrl,
                PlacementType = null
            }).ToList();

            // Kết hợp: Quảng cáo trước (priority cao), special events sau
            result = rotatedAds.Concat(specialEventResponses).ToList();
            
            _logger.LogInformation(
                "Retrieved {TotalCount} items: {AdCount} advertisement(s) + {SpecialEventCount} special event(s) (PlacementType: all, RotationIndex: {RotationIndex})",
                result.Count, rotatedAds.Count, specialEventResponses.Count, _rotationIndex);
        }
        else if (placementType.Trim().ToUpper() == "POPUP")
        {
            // POP_UP: Chỉ trả về 1 quảng cáo, lấy ngẫu nhiên
            if (rotatedAds.Any())
            {
                int randomIndex;
                lock (_lock)
                {
                    randomIndex = _random.Next(rotatedAds.Count);
                }
                result = new List<AdvertisementResponse> { rotatedAds[randomIndex] };
                
                _logger.LogInformation(
                    "Retrieved 1 random POP_UP advertisement (Index: {RandomIndex}/{TotalCount})",
                    randomIndex, rotatedAds.Count);
            }
            else
            {
                result = new List<AdvertisementResponse>();
                _logger.LogInformation("No POP_UP advertisements found");
            }
        }
        else
        {
            // Các placementType khác: Trả về toàn bộ quảng cáo đã filter
            result = rotatedAds;
            
            _logger.LogInformation(
                "Retrieved {TotalCount} advertisement(s) for PlacementType '{PlacementType}' (RotationIndex: {RotationIndex})",
                result.Count, placementType, _rotationIndex);
        }

        return result;
    }

    public async Task<AdvertisementDetailResponse> CreateAdvertisementAsync(CreateAdvertisementRequest request, int userId)
    {
        _logger.LogInformation("Creating advertisement for user {UserId}", userId);

        // Find VenueOwnerProfile from userId
        var venueOwnerProfile = await _unitOfWork.Context.Set<VenueOwnerProfile>()
            .FirstOrDefaultAsync(vop => vop.UserId == userId && vop.IsDeleted != true);

        if (venueOwnerProfile == null)
        {
            _logger.LogError("User {UserId} does not have a venue owner profile", userId);
            throw new InvalidOperationException("You are not registered as a venue owner. Please create a venue owner profile first.");
        }

        // Validate desired start date
        var now = DateTime.UtcNow;
        if (request.DesiredStartDate < now.AddHours(-1)) // Allow 1 hour grace period for timezone issues
        {
            throw new InvalidOperationException("Desired start date cannot be in the past");
        }

        if (request.DesiredStartDate > now.AddYears(1))
        {
            throw new InvalidOperationException("Desired start date cannot be more than 1 year in the future");
        }

        // Create advertisement (venue will be assigned when submitting payment)
        var advertisement = new Advertisement
        {
            VenueOwnerId = venueOwnerProfile.Id,
            Title = request.Title,
            Content = request.Content,
            BannerUrl = request.BannerUrl,
            TargetUrl = request.TargetUrl,
            PlacementType = request.PlacementType,
            DesiredStartDate = request.DesiredStartDate,
            Status = AdvertisementStatus.DRAFT.ToString(), // Default to DRAFT, will be updated to PENDING after payment
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Advertisements.AddAsync(advertisement);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created advertisement ID {AdId} for venue owner {VenueOwnerId} with DesiredStartDate {StartDate}", 
            advertisement.Id, venueOwnerProfile.Id, request.DesiredStartDate);

        // Load with details for response
        var created = await _unitOfWork.Advertisements.GetByIdWithDetailsAsync(advertisement.Id);
        
        return MapToDetailResponse(created!);
    }

    public async Task<AdvertisementDetailResponse> UpdateAdvertisementAndRevertToDraftAsync(
        int advertisementId, 
        int userId, 
        UpdateAdvertisementRequest request)
    {
        var venueOwnerProfile = await _unitOfWork.Context.Set<VenueOwnerProfile>()
            .FirstOrDefaultAsync(vop => vop.UserId == userId && vop.IsDeleted != true);

        if (venueOwnerProfile == null)
        {
            throw new InvalidOperationException("You are not registered as a venue owner");
        }

        var advertisement = await _unitOfWork.Advertisements.GetByIdWithDetailsAsync(advertisementId);

        if (advertisement == null || advertisement.IsDeleted == true)
        {
            throw new InvalidOperationException("Advertisement not found");
        }

        if (advertisement.VenueOwnerId != venueOwnerProfile.Id)
        {
            throw new InvalidOperationException("You don't have permission to update this advertisement");
        }

        if (advertisement.Status != AdvertisementStatus.REJECTED.ToString() && advertisement.Status != AdvertisementStatus.DRAFT.ToString())
        {
            throw new InvalidOperationException($"Can only update REJECTED or DRAFT advertisements. Current status: {advertisement.Status}");
        }

        var now = DateTime.UtcNow;
        if (request.DesiredStartDate < now.AddHours(-1))
        {
            throw new InvalidOperationException("Desired start date cannot be in the past");
        }

        if (request.DesiredStartDate > now.AddYears(1))
        {
            throw new InvalidOperationException("Desired start date cannot be more than 1 year in the future");
        }

        using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync();

        try
        {
            advertisement.Title = request.Title;
            advertisement.Content = request.Content;
            advertisement.BannerUrl = request.BannerUrl;
            advertisement.TargetUrl = request.TargetUrl;
            advertisement.PlacementType = request.PlacementType;
            advertisement.DesiredStartDate = request.DesiredStartDate;
            advertisement.Status = AdvertisementStatus.DRAFT.ToString();
            advertisement.UpdatedAt = DateTime.UtcNow;

            if (advertisement.VenueLocationAdvertisements != null && advertisement.VenueLocationAdvertisements.Any())
            {
                foreach (var vla in advertisement.VenueLocationAdvertisements)
                {
                    vla.Status = VenueLocationAdvertisementStatus.CANCELLED.ToString();
                    vla.UpdatedAt = DateTime.UtcNow;
                }
            }

            _unitOfWork.Advertisements.Update(advertisement);
            await _unitOfWork.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            var updated = await _unitOfWork.Advertisements.GetByIdWithDetailsAsync(advertisementId);
            
            var rejectionHistory = ParseRejectionHistory(updated!.RejectionReason);
            
            return new AdvertisementDetailResponse
            {
                Id = updated.Id,
                VenueOwnerId = updated.VenueOwnerId,
                Title = updated.Title ?? string.Empty,
                Content = updated.Content,
                BannerUrl = updated.BannerUrl ?? string.Empty,
                TargetUrl = updated.TargetUrl,
                PlacementType = updated.PlacementType ?? string.Empty,
                Status = updated.Status ?? AdvertisementStatus.DRAFT.ToString(),
                RejectionHistory = rejectionHistory,
                DesiredStartDate = updated.DesiredStartDate,
                CreatedAt = updated.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = updated.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<MyAdvertisementResponse>> GetMyAdvertisementsAsync(int userId)
    {
        _logger.LogInformation("Getting advertisements for user {UserId}", userId);

        // Find VenueOwnerProfile from userId
        var venueOwnerProfile = await _unitOfWork.Context.Set<VenueOwnerProfile>()
            .FirstOrDefaultAsync(vop => vop.UserId == userId && vop.IsDeleted != true);

        if (venueOwnerProfile == null)
        {
            _logger.LogWarning("User {UserId} does not have a venue owner profile", userId);
            return new List<MyAdvertisementResponse>();
        }

        var advertisements = await _unitOfWork.Advertisements.GetByVenueOwnerIdAsync(venueOwnerProfile.Id);

        var responses = advertisements.Select(ad =>
        {
            var activeVenueAd = ad.VenueLocationAdvertisements
                .Where(vla => vla.Status == VenueLocationAdvertisementStatus.ACTIVE.ToString() && vla.EndDate >= DateTime.UtcNow)
                .OrderByDescending(vla => vla.StartDate)
                .FirstOrDefault();

            return new MyAdvertisementResponse
            {
                Id = ad.Id,
                Title = ad.Title ?? string.Empty,
                BannerUrl = ad.BannerUrl ?? string.Empty,
                PlacementType = ad.PlacementType ?? string.Empty,
                Status = ad.Status ?? AdvertisementStatus.DRAFT.ToString(),
                RejectionHistory = ParseRejectionHistory(ad.RejectionReason),
                DesiredStartDate = ad.DesiredStartDate,
                CreatedAt = ad.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = ad.UpdatedAt,
                VenueLocationCount = ad.VenueLocationAdvertisements.Count,
                ActiveVenueAd = activeVenueAd != null ? new ActiveVenueLocationAd
                {
                    Id = activeVenueAd.Id,
                    VenueId = activeVenueAd.VenueId,
                    VenueName = activeVenueAd.Venue?.Name ?? "Unknown",
                    StartDate = activeVenueAd.StartDate,
                    EndDate = activeVenueAd.EndDate,
                    PriorityScore = activeVenueAd.PriorityScore
                } : null
            };
        }).ToList();

        return responses;
    }

    public async Task<AdvertisementDetailResponse?> GetAdvertisementByIdAsync(int id, int userId)
    {
        _logger.LogInformation("Getting advertisement {AdId} for user {UserId}", id, userId);

        // Find VenueOwnerProfile from userId
        var venueOwnerProfile = await _unitOfWork.Context.Set<VenueOwnerProfile>()
            .FirstOrDefaultAsync(vop => vop.UserId == userId && vop.IsDeleted != true);

        if (venueOwnerProfile == null)
        {
            _logger.LogWarning("User {UserId} does not have a venue owner profile", userId);
            return null;
        }

        var advertisement = await _unitOfWork.Advertisements.GetByIdWithDetailsAsync(id);

        if (advertisement == null || advertisement.VenueOwnerId != venueOwnerProfile.Id)
        {
            return null;
        }

        return MapToDetailResponse(advertisement);
    }

    public async Task<SubmitAdvertisementWithPaymentResponse> SubmitAdvertisementWithPaymentAsync(
        int advertisementId, 
        int userId, 
        SubmitAdvertisementWithPaymentRequest request)
    {
        _logger.LogInformation("Submitting advertisement {AdId} with payment - UserId: {UserId}, PackageId: {PackageId}",
            advertisementId, userId, request.PackageId);

        // 1. Find VenueOwnerProfile from userId
        var venueOwnerProfile = await _unitOfWork.Context.Set<VenueOwnerProfile>()
            .FirstOrDefaultAsync(vop => vop.UserId == userId && vop.IsDeleted != true);

        if (venueOwnerProfile == null)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "You are not registered as a venue owner"
            };
        }

        // 2. Validate advertisement
        var advertisement = await _unitOfWork.Advertisements.GetByIdWithDetailsAsync(advertisementId);

        if (advertisement == null || advertisement.IsDeleted == true)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "Advertisement not found"
            };
        }

        if (advertisement.VenueOwnerId != venueOwnerProfile.Id)
        {
            _logger.LogWarning("User {UserId} attempted to submit advertisement {AdId} but is not the owner", 
                userId, advertisementId);
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "Unauthorized access"
            };
        }

        // 3. Validate status - Allow DRAFT or REJECTED
        if (advertisement.Status != AdvertisementStatus.DRAFT.ToString() 
            && advertisement.Status != AdvertisementStatus.REJECTED.ToString())
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = $"Advertisement status is {advertisement.Status}, cannot submit. Only DRAFT or REJECTED advertisements can be submitted."
            };
        }

        // 3.5. Check rejection count - Block if rejected more than 5 times
        if (advertisement.Status == AdvertisementStatus.REJECTED.ToString())
        {
            var rejectionHistory = ParseRejectionHistory(advertisement.RejectionReason);
            if (rejectionHistory != null && rejectionHistory.Count >= 5)
            {
                return new SubmitAdvertisementWithPaymentResponse
                {
                    IsSuccess = false,
                    Message = $"This advertisement has been rejected {rejectionHistory.Count} times. Maximum 5 rejections allowed. Please contact support for assistance."
                };
            }
        }

        // 4. Validate required fields
        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(advertisement.Title)) missingFields.Add("Title");
        if (string.IsNullOrWhiteSpace(advertisement.BannerUrl)) missingFields.Add("BannerUrl");
        if (string.IsNullOrWhiteSpace(advertisement.PlacementType)) missingFields.Add("PlacementType");

        if (missingFields.Any())
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "Please fill in all required fields before submitting.",
                MissingFields = missingFields
            };
        }

        // 5. Validate package
        var package = await _unitOfWork.Context.Set<AdvertisementPackage>()
            .FirstOrDefaultAsync(p => p.Id == request.PackageId
                && p.IsDeleted != true
                && p.IsActive == true);

        if (package == null)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "Advertisement package not found or inactive"
            };
        }

        if (package.Price <= 0 || package.DurationDays <= 0)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "Package configuration is invalid"
            };
        }

        // 6. Check if there's already a pending payment
        var existingPending = await _unitOfWork.Context.Set<AdsOrder>()
            .Where(ao => ao.AdvertisementId == advertisementId
                && ao.Status == AdsOrderStatus.PENDING.ToString()
                && ao.CreatedAt > DateTime.UtcNow.AddMinutes(-15))
            .FirstOrDefaultAsync();

        if (existingPending != null)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "There is already a pending payment for this advertisement. Please complete or wait for it to expire."
            };
        }

        // 7. Validate venues from request
        if (request.VenueIds == null || !request.VenueIds.Any())
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "At least one VenueId is required"
            };
        }

        // Remove duplicates
        var uniqueVenueIds = request.VenueIds.Distinct().ToList();

        var venues = await _unitOfWork.Context.Set<VenueLocation>()
            .Where(v => uniqueVenueIds.Contains(v.Id) 
                && v.IsDeleted != true
                && v.Status == VenueLocationStatus.ACTIVE.ToString())
            .ToListAsync();

        if (venues.Count != uniqueVenueIds.Count)
        {
            var foundIds = venues.Select(v => v.Id).ToList();
            var missingIds = uniqueVenueIds.Except(foundIds).ToList();
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = $"Venue(s) not found or not active: {string.Join(", ", missingIds)}"
            };
        }

        // Check ownership for all venues
        var notOwnedVenues = venues.Where(v => v.VenueOwnerId != venueOwnerProfile.Id).ToList();
        if (notOwnedVenues.Any())
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = $"You can only create advertisements for your own venues. Unauthorized venues: {string.Join(", ", notOwnedVenues.Select(v => v.Name))}"
            };
        }

        // 7.5. Get desired start date from advertisement
        if (!advertisement.DesiredStartDate.HasValue)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "Advertisement does not have a desired start date. Please recreate the advertisement."
            };
        }

        var desiredStartDate = advertisement.DesiredStartDate.Value;

        // 7.6. Validate desired start date
        var minStartDate = DateTime.UtcNow.Date; // Must be at least today
        var maxStartDate = DateTime.UtcNow.AddMonths(6); // Maximum 6 months in advance

        if (desiredStartDate.Date < minStartDate)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = $"Desired start date cannot be in the past. Minimum date: {minStartDate:yyyy-MM-dd}"
            };
        }

        if (desiredStartDate > maxStartDate)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = $"Desired start date is too far in the future. Maximum date: {maxStartDate:yyyy-MM-dd}"
            };
        }

        // 8. Calculate amount (same price for all venues in the package)
        var totalAmount = package.Price;

        // 8.5. Validate payment method
        var paymentMethod = request.PaymentMethod?.ToUpper() ?? "VIETQR";
        if (paymentMethod != "VIETQR" && paymentMethod != "WALLET")
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "Invalid payment method. Must be VIETQR or WALLET"
            };
        }

        // 8.6. If WALLET, check balance first
        if (paymentMethod == "WALLET")
        {
            var (hasSufficient, currentBalance) = await _walletPaymentService.CheckWalletBalanceAsync(userId, totalAmount);
            if (!hasSufficient)
            {
                return new SubmitAdvertisementWithPaymentResponse
                {
                    IsSuccess = false,
                    Message = $"Insufficient wallet balance. Available: {currentBalance:N0} VND, Required: {totalAmount:N0} VND"
                };
            }
        }

        // 9. Start transaction with SERIALIZABLE isolation (prevent race condition)
        using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        try
        {
            // 10. Create AdsOrder (PENDING)
            var adsOrder = new AdsOrder
            {
                PackageId = request.PackageId,
                AdvertisementId = advertisementId,
                PricePaid = null, // Will be set after payment confirmation
                Status = AdsOrderStatus.PENDING.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Context.Set<AdsOrder>().AddAsync(adsOrder);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Created AdsOrder ID: {OrderId}", adsOrder.Id);

            // 10.5 Remove existing VenueLocationAdvertisement records for this advertisement to prevent duplicates
            var existingVenueLocationAds = await _unitOfWork.Context.Set<VenueLocationAdvertisement>()
                .Where(vla => vla.AdvertisementId == advertisementId)
                .ToListAsync();

            if (existingVenueLocationAds.Any())
            {
                _logger.LogInformation("Removing {Count} existing VenueLocationAdvertisement(s) for Advertisement {AdId} to prevent duplicates",
                    existingVenueLocationAds.Count, advertisementId);
                _unitOfWork.Context.Set<VenueLocationAdvertisement>().RemoveRange(existingVenueLocationAds);
                await _unitOfWork.SaveChangesAsync();
            }

            // 10.6 Create VenueLocationAdvertisement for each venue with desired start date
            var venueLocationAds = new List<VenueLocationAdvertisement>();
            
            foreach (var venue in venues)
            {
                var venueLocationAd = new VenueLocationAdvertisement
                {
                    AdvertisementId = advertisementId,
                    VenueId = venue.Id,
                    PriorityScore = package.PriorityScore, // Set from package
                    StartDate = desiredStartDate, // Use desired start date from advertisement
                    EndDate = desiredStartDate.AddDays(package.DurationDays),
                    Status = VenueLocationAdvertisementStatus.PENDING_PAYMENT.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                venueLocationAds.Add(venueLocationAd);
            }

            await _unitOfWork.Context.Set<VenueLocationAdvertisement>().AddRangeAsync(venueLocationAds);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Created {Count} VenueLocationAdvertisement(s) for VenueIds: {VenueIds}, DesiredStartDate: {StartDate}", 
                venueLocationAds.Count, string.Join(", ", venues.Select(v => v.Id)), desiredStartDate);

            // 11. Create Transaction
            var paymentContent = $"ADO{adsOrder.Id}";
            var venueNames = string.Join(", ", venues.Select(v => v.Name));

            var transaction = new Transaction
            {
                UserId = userId,
                Amount = totalAmount,
                Currency = "VND",
                PaymentMethod = paymentMethod,
                TransType = (int)TransactionType.ADS_ORDER,
                DocNo = adsOrder.Id,
                Description = $"Thanh toán quảng cáo {package.Name} cho {venues.Count} địa điểm: {venueNames}",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Context.Set<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Created transaction ID: {TxId} for {Count} venue(s), PaymentMethod: {Method}", 
                transaction.Id, venues.Count, paymentMethod);

            // ========== WALLET PAYMENT FLOW ==========
            if (paymentMethod == "WALLET")
            {
                // Process wallet payment immediately
                var walletResult = await _walletPaymentService.ProcessWalletPaymentAsync(
                    userId,
                    totalAmount,
                    transaction.Id,
                    transaction.Description ?? "Advertisement payment");

                if (!walletResult.IsSuccess)
                {
                    await dbTransaction.RollbackAsync();
                    return new SubmitAdvertisementWithPaymentResponse
                    {
                        IsSuccess = false,
                        Message = walletResult.Message
                    };
                }

                // Update AdsOrder to COMPLETED
                var now = DateTime.UtcNow;
                adsOrder.Status = AdsOrderStatus.COMPLETED.ToString();
                adsOrder.PricePaid = totalAmount;
                adsOrder.UpdatedAt = now;
                _unitOfWork.Context.Set<AdsOrder>().Update(adsOrder);

                // Update Advertisement status to PENDING
                advertisement.Status = AdvertisementStatus.PENDING.ToString();
                advertisement.UpdatedAt = now;
                _unitOfWork.Context.Set<Advertisement>().Update(advertisement);

                // Update VenueLocationAdvertisement status to PENDING
                foreach (var vla in venueLocationAds)
                {
                    vla.Status = VenueLocationAdvertisementStatus.PENDING.ToString();
                    vla.UpdatedAt = now;
                    _unitOfWork.Context.Set<VenueLocationAdvertisement>().Update(vla);
                }

                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();



                return new SubmitAdvertisementWithPaymentResponse
                {
                    IsSuccess = true,
                    Message = $"Payment successful via Wallet. Advertisement submitted for admin approval. Balance: {walletResult.OldBalance:N0} → {walletResult.NewBalance:N0} VND",
                    TransactionId = transaction.Id,
                    AdsOrderId = adsOrder.Id,
                    QrCodeUrl = null, // No QR for wallet payment
                    Amount = totalAmount,
                    BankInfo = null,
                    ExpireAt = null,
                    PaymentContent = paymentContent,
                    PackageName = package.Name,
                    DurationDays = package.DurationDays,
                    PaymentMethod = "WALLET",
                    WalletBalance = walletResult.NewBalance
                };
            }

            // ========== VIETQR PAYMENT FLOW (ORIGINAL LOGIC) ==========
            // 12. Create Sepay transaction
            SepayTransactionResponse sepayResponse;
            try
            {
                // Order code = ADO{adsOrderId} để tracking
                sepayResponse = await _sepayService.CreateTransactionAsync(
                    totalAmount,
                    paymentContent,
                    $"ADO{adsOrder.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create Sepay transaction");
                await dbTransaction.RollbackAsync();
                return new SubmitAdvertisementWithPaymentResponse
                {
                    IsSuccess = false,
                    Message = "Unable to create payment transaction. Please try again."
                };
            }

            if (sepayResponse.Data == null || string.IsNullOrEmpty(sepayResponse.Data.QrCode))
            {
                _logger.LogError("❌ Sepay response invalid - no QR code");
                await dbTransaction.RollbackAsync();
                return new SubmitAdvertisementWithPaymentResponse
                {
                    IsSuccess = false,
                    Message = "Failed to generate QR code. Please try again."
                };
            }

            // 13. Update transaction with VietQR info
            var expireAt = DateTime.UtcNow.AddMinutes(15);
            var (bankName, accountNumber, accountName) = _sepayService.GetBankInfo();
            
            var externalRef = System.Text.Json.JsonSerializer.Serialize(new
            {
                sepayTransactionId = sepayResponse.Data.Id,
                qrCodeUrl = sepayResponse.Data.QrCode, // VietQR image URL
                qrData = sepayResponse.Data.QrData,
                orderCode = sepayResponse.Data.OrderCode,
                venueIds = uniqueVenueIds, // Store all venueIds for webhook processing
                expireAt,
                bankInfo = new { bankName, accountNumber, accountName }
            });

            transaction.ExternalRefCode = externalRef;
            transaction.UpdatedAt = DateTime.UtcNow;
            
            _unitOfWork.Context.Set<Transaction>().Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            // 14. Commit transaction
            await dbTransaction.CommitAsync();

            _logger.LogInformation("✅ VIETQR payment initiated - TxId: {TxId}, AdsOrderId: {AdsOrderId}, SepayId: {SepayId}, VenueCount: {Count}, VenueIds: {VenueIds}", 
                transaction.Id, adsOrder.Id, sepayResponse.Data.Id, venues.Count, string.Join(", ", uniqueVenueIds));

            // 15. Return response with QR code
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = true,
                Message = "Advertisement validated successfully. Please complete payment to submit for approval.",
                TransactionId = transaction.Id,
                AdsOrderId = adsOrder.Id,
                QrCodeUrl = sepayResponse.Data.QrCode, // VietQR image URL
                Amount = totalAmount,
                BankInfo = new BankInfo
                {
                    BankName = bankName,
                    AccountNumber = accountNumber,
                    AccountName = accountName
                },
                ExpireAt = expireAt,
                PaymentContent = paymentContent,
                PackageName = package.Name,
                DurationDays = package.DurationDays,
                PaymentMethod = "VIETQR"
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "❌ Error during payment creation for advertisement {AdId}", advertisementId);
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "An error occurred while processing payment. Please try again."
            };
        }
    }

    public async Task<GroupedAdvertisementPackagesResponse> GetAdvertisementPackagesAsync()
    {
        _logger.LogInformation("Getting all active advertisement packages");

        var packages = await _unitOfWork.Context.Set<AdvertisementPackage>()
            .Where(p => p.IsDeleted != true && p.IsActive == true)
            .OrderBy(p => p.Price)
            .ToListAsync();

        var responses = packages.Select(p => new AdvertisementPackageResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            DurationDays = p.DurationDays,
            PriorityScore = p.PriorityScore,
            Placement = p.Placement,
            IsActive = p.IsActive ?? true,
            CreatedAt = p.CreatedAt ?? DateTime.UtcNow
        }).ToList();

        // Group by Placement
        var grouped = responses
            .GroupBy(p => p.Placement ?? "OTHER")
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(p => p.PriorityScore).ToList()
            );

        var totalCount = responses.Count;
        var placementGroups = grouped.Count;
        
        _logger.LogInformation("Found {Count} active advertisement packages grouped into {Groups} placement(s)", 
            totalCount, placementGroups);

        return new GroupedAdvertisementPackagesResponse
        {
            Data = grouped
        };
    }

    #region Helper Methods

    private AdvertisementDetailResponse MapToDetailResponse(Advertisement ad)
    {
        // Parse rejection history
        var rejectionHistory = ParseRejectionHistory(ad.RejectionReason);

        return new AdvertisementDetailResponse
        {
            Id = ad.Id,
            VenueOwnerId = ad.VenueOwnerId,
            Title = ad.Title ?? string.Empty,
            Content = ad.Content,
            BannerUrl = ad.BannerUrl ?? string.Empty,
            TargetUrl = ad.TargetUrl,
            PlacementType = ad.PlacementType ?? string.Empty,
            Status = ad.Status ?? AdvertisementStatus.DRAFT.ToString(),
            RejectionHistory = rejectionHistory,
            DesiredStartDate = ad.DesiredStartDate,
            CreatedAt = ad.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = ad.UpdatedAt,
            VenueLocationAds = ad.VenueLocationAdvertisements?.Select(vla => new VenueLocationAdInfo
            {
                Id = vla.Id,
                VenueId = vla.VenueId,
                VenueName = vla.Venue?.Name ?? "Unknown",
                PriorityScore = vla.PriorityScore,
                StartDate = vla.StartDate,
                EndDate = vla.EndDate,
                Status = vla.Status ?? string.Empty
            }).ToList(),
            AdsOrders = ad.AdsOrders?.Select(ao => new AdsOrderInfo
            {
                Id = ao.Id,
                PackageName = ao.Package?.Name ?? "Unknown",
                PricePaid = ao.PricePaid,
                Status = ao.Status ?? string.Empty,
                CreatedAt = ao.CreatedAt ?? DateTime.UtcNow
            }).ToList()
        };
    }

    private List<RejectionHistoryEntry>? ParseRejectionHistory(string? rejectionReason)
    {
        if (string.IsNullOrEmpty(rejectionReason))
            return null;

        try
        {
            var history = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(rejectionReason);
            if (history == null)
                return null;

            return history.Select(entry =>
            {
                var rejectedAt = DateTime.UtcNow;
                var reason = "No reason provided";
                var rejectedBy = "Unknown";

                if (entry.ContainsKey("rejectedAt"))
                {
                    DateTime.TryParse(entry["rejectedAt"]?.ToString(), out rejectedAt);
                }
                if (entry.ContainsKey("reason"))
                {
                    reason = entry["reason"]?.ToString() ?? reason;
                }
                if (entry.ContainsKey("rejectedBy"))
                {
                    rejectedBy = entry["rejectedBy"]?.ToString() ?? rejectedBy;
                }

                return new RejectionHistoryEntry
                {
                    RejectedAt = rejectedAt,
                    Reason = reason,
                    RejectedBy = rejectedBy
                };
            }).ToList();
        }
        catch
        {
            // If not JSON format, return as single entry (backward compatibility)
            return new List<RejectionHistoryEntry>
            {
                new RejectionHistoryEntry
                {
                    RejectedAt = DateTime.UtcNow,
                    Reason = rejectionReason,
                    RejectedBy = "Unknown"
                }
            };
        }
    }

    #endregion

    #region Admin Advertisement Management

    public async Task<AdvertisementApprovalResult> ApproveAdvertisementAsync(ApproveAdvertisementRequest request)
    {
        _logger.LogInformation("Admin approving advertisement {AdId}", request.AdvertisementId);

        var advertisement = await _unitOfWork.Advertisements.GetByIdWithDetailsAsync(request.AdvertisementId);
        
        if (advertisement == null)
        {
            return new AdvertisementApprovalResult 
            { 
                IsSuccess = false, 
                Message = "Advertisement not found" 
            };
        }

        if (advertisement.Status != AdvertisementStatus.PENDING.ToString())
        {
            return new AdvertisementApprovalResult 
            { 
                IsSuccess = false, 
                Message = $"Cannot approve advertisement with status '{advertisement.Status}'. Only 'PENDING' advertisements can be processed." 
            };
        }

        advertisement.Status = AdvertisementStatus.APPROVED.ToString();
        advertisement.UpdatedAt = DateTime.UtcNow;
        advertisement.RejectionReason = null;
        
        var approvalDate = DateTime.UtcNow;
        var adjustedCount = 0;
        
        if (advertisement.VenueLocationAdvertisements != null && advertisement.VenueLocationAdvertisements.Any())
        {
            // Process PENDING status
            foreach (var vla in advertisement.VenueLocationAdvertisements
                .Where(v => v.Status == VenueLocationAdvertisementStatus.PENDING.ToString()))
            {
                // Auto-adjust dates if admin approves after desired start date
                if (approvalDate > vla.StartDate)
                {
                    var originalStart = vla.StartDate;
                    var originalEnd = vla.EndDate;
                    var duration = (vla.EndDate - vla.StartDate).Days;
                    
                    // Adjust to start from approval date
                    vla.StartDate = approvalDate;
                    vla.EndDate = approvalDate.AddDays(duration);
                    
                    adjustedCount++;
                    _logger.LogInformation(
                        "Auto-adjusted VenueLocationAd {VlaId}: StartDate {OriginalStart} → {NewStart}, EndDate {OriginalEnd} → {NewEnd} (Approved late by {DelayDays} days)",
                        vla.Id, originalStart, vla.StartDate, originalEnd, vla.EndDate, (approvalDate - originalStart).Days);
                }
                
                vla.Status = VenueLocationAdvertisementStatus.ACTIVE.ToString();
                vla.UpdatedAt = approvalDate;
            }
        }

        _unitOfWork.Advertisements.Update(advertisement);
        await _unitOfWork.SaveChangesAsync();

        var message = adjustedCount > 0
            ? $"Advertisement approved successfully. {adjustedCount} venue location(s) had start dates auto-adjusted due to late approval."
            : "Advertisement approved successfully";

        _logger.LogInformation("Advertisement {AdId} approved and activated. Adjusted venues: {Count}", 
            request.AdvertisementId, adjustedCount);

        return new AdvertisementApprovalResult 
        { 
            IsSuccess = true, 
            Message = message
        };
    }

    public async Task<AdvertisementApprovalResult> RejectAdvertisementAsync(RejectAdvertisementRequest request)
    {
        _logger.LogInformation("Admin rejecting advertisement {AdId}", request.AdvertisementId);

        var advertisement = await _unitOfWork.Advertisements.GetByIdWithDetailsAsync(request.AdvertisementId);
        
        if (advertisement == null)
        {
            return new AdvertisementApprovalResult 
            { 
                IsSuccess = false, 
                Message = "Advertisement not found" 
            };
        }

        if (advertisement.Status != AdvertisementStatus.PENDING.ToString())
        {
            return new AdvertisementApprovalResult 
            { 
                IsSuccess = false, 
                Message = $"Cannot reject advertisement with status '{advertisement.Status}'. Only 'PENDING' advertisements can be processed." 
            };
        }

        using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
        
        try
        {
            var now = DateTime.UtcNow;
            
            // 1. Build rejection history (support multiple rejections)
            var rejectionHistory = new List<Dictionary<string, object>>();
            
            // Parse existing rejection history
            if (!string.IsNullOrEmpty(advertisement.RejectionReason))
            {
                try
                {
                    var existingHistory = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                        advertisement.RejectionReason);
                    
                    if (existingHistory != null)
                    {
                        rejectionHistory = existingHistory;
                    }
                }
                catch
                {
                    // If old format (plain string), convert to history format
                    rejectionHistory.Add(new Dictionary<string, object>
                    {
                        { "rejectedAt", (advertisement.UpdatedAt ?? now).ToString("O") },
                        { "reason", advertisement.RejectionReason },
                        { "rejectedBy", "Unknown" }
                    });
                }
            }
            
            // Add new rejection entry
            rejectionHistory.Add(new Dictionary<string, object>
            {
                { "rejectedAt", now.ToString("O") },
                { "reason", request.Reason ?? "No reason provided" },
                { "rejectedBy", "Admin" } // TODO: Get actual admin email/ID from JWT
            });
            
            // 2. Update advertisement status
            advertisement.Status = AdvertisementStatus.REJECTED.ToString();
            advertisement.UpdatedAt = now;
            advertisement.RejectionReason = System.Text.Json.JsonSerializer.Serialize(rejectionHistory);

            // 3. Cancel all VenueLocationAdvertisements
            if (advertisement.VenueLocationAdvertisements != null && advertisement.VenueLocationAdvertisements.Any())
            {
                foreach (var vla in advertisement.VenueLocationAdvertisements)
                {
                    vla.Status = VenueLocationAdvertisementStatus.CANCELLED.ToString();
                    vla.UpdatedAt = now;
                }
            }

            // 3. Find the completed AdsOrder and process refund
            var completedOrder = await _unitOfWork.Context.Set<AdsOrder>()
                .FirstOrDefaultAsync(ao => ao.AdvertisementId == request.AdvertisementId 
                    && ao.Status == AdsOrderStatus.COMPLETED.ToString());

            string refundMessage = "";
            
            if (completedOrder != null && completedOrder.PricePaid.HasValue && completedOrder.PricePaid.Value > 0)
            {
                // Find the successful transaction
                var successfulTransaction = await _unitOfWork.Context.Set<Transaction>()
                    .FirstOrDefaultAsync(t => t.TransType == (int)TransactionType.ADS_ORDER 
                        && t.DocNo == completedOrder.Id
                        && t.Status == TransactionStatus.SUCCESS.ToString());

                if (successfulTransaction != null)
                {
                    // Get VenueOwner's UserId for refund
                    var venueOwner = await _unitOfWork.Context.Set<VenueOwnerProfile>()
                        .FirstOrDefaultAsync(vop => vop.Id == advertisement.VenueOwnerId);

                    if (venueOwner != null)
                    {
                        // Use RefundService to process refund to wallet
                        var refundMetadata = new Dictionary<string, object>
                        {
                            { "advertisementId", advertisement.Id },
                            { "advertisementTitle", advertisement.Title ?? "" },
                            { "rejectionReason", request.Reason ?? "Advertisement rejected by admin" }
                        };

                        var refundResult = await _refundService.ProcessRefundAsync(
                            userId: venueOwner.UserId,
                            amount: completedOrder.PricePaid.Value,
                            transType: (int)TransactionType.ADS_ORDER,
                            docNo: completedOrder.Id,
                            reason: $"Quảng cáo '{advertisement.Title}' bị từ chối. Lý do: {request.Reason ?? "Không đạt yêu cầu"}",
                            originalTransactionId: successfulTransaction.Id,
                            metadata: refundMetadata
                        );

                        if (refundResult.IsSuccess)
                        {
                            // Update AdsOrder status to REFUNDED
                            completedOrder.Status = AdsOrderStatus.REFUNDED.ToString();
                            completedOrder.UpdatedAt = now;
                            _unitOfWork.Context.Set<AdsOrder>().Update(completedOrder);

                            refundMessage = $" Đã hoàn {refundResult.RefundAmount:N0} VND vào ví (Balance: {refundResult.OldBalance:N0} → {refundResult.NewBalance:N0} VND).";
                            
                            _logger.LogInformation(
                                "✅ Refund processed for advertisement {AdId}. Amount: {Amount} VND, UserId: {UserId}, TxId: {TxId}",
                                request.AdvertisementId, completedOrder.PricePaid.Value, venueOwner.UserId, refundResult.TransactionId);
                        }
                        else
                        {
                            _logger.LogError("Failed to process refund for advertisement {AdId}: {Message}", 
                                request.AdvertisementId, refundResult.Message);
                            await dbTransaction.RollbackAsync();
                            
                            return new AdvertisementApprovalResult 
                            { 
                                IsSuccess = false, 
                                Message = $"Failed to process refund: {refundResult.Message}" 
                            };
                        }
                    }
                    else
                    {
                        _logger.LogWarning("VenueOwner not found for advertisement {AdId}, cannot process refund", request.AdvertisementId);
                    }
                }
                else
                {
                    _logger.LogWarning("No successful transaction found for AdsOrder {OrderId}", completedOrder.Id);
                }
            }
            else
            {
                _logger.LogInformation("No completed order with payment found for advertisement {AdId}, no refund needed", request.AdvertisementId);
            }

            _unitOfWork.Advertisements.Update(advertisement);
            await _unitOfWork.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation("Advertisement {AdId} rejected. Reason: {Reason}", 
                request.AdvertisementId, request.Reason ?? "No reason provided");

            return new AdvertisementApprovalResult 
            { 
                IsSuccess = true, 
                Message = $"Advertisement rejected successfully.{refundMessage}"
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error rejecting advertisement {AdId} with refund", request.AdvertisementId);
            
            return new AdvertisementApprovalResult 
            { 
                IsSuccess = false, 
                Message = "Failed to reject advertisement and process refund" 
            };
        }
    }

    #endregion

    #region Public Detail APIs

    public async Task<PublicAdvertisementDetailResponse> GetPublicAdvertisementDetailAsync(int advertisementId)
    {
        _logger.LogInformation("Getting public advertisement detail for ID {AdId}", advertisementId);

        var venueLocationAds = await _unitOfWork.Context.Set<VenueLocationAdvertisement>()
            .Include(vla => vla.Advertisement)
            .Include(vla => vla.Venue)
            .Where(vla => vla.AdvertisementId == advertisementId 
                && vla.Status == VenueLocationAdvertisementStatus.ACTIVE.ToString()
                && vla.Advertisement.IsDeleted != true
                && vla.Advertisement.Status == AdvertisementStatus.APPROVED.ToString())
            .ToListAsync();

        if (venueLocationAds == null || !venueLocationAds.Any())
        {
            throw new KeyNotFoundException($"Không tìm thấy quảng cáo với ID {advertisementId}");
        }

        var firstAd = venueLocationAds.First();
        var ad = firstAd.Advertisement;

        var venues = venueLocationAds.Select(vla => new VenueDetailInfo
        {
            VenueId = vla.Venue.Id,
            VenueName = vla.Venue.Name ?? string.Empty,
            VenueDescription = vla.Venue.Description,
            VenueAddress = vla.Venue.Address ?? string.Empty,
            VenuePhoneNumber = vla.Venue.PhoneNumber,
            VenueEmail = vla.Venue.Email,
            VenueWebsiteUrl = vla.Venue.WebsiteUrl,
            VenuePriceMin = vla.Venue.PriceMin,
            VenuePriceMax = vla.Venue.PriceMax,
            VenueLatitude = vla.Venue.Latitude,
            VenueLongitude = vla.Venue.Longitude,
            VenueAverageRating = vla.Venue.AverageRating,
            VenueReviewCount = vla.Venue.ReviewCount,
            VenueCoverImage = ParseImageField(vla.Venue.CoverImage),
            VenueInteriorImage = ParseImageField(vla.Venue.InteriorImage),
            VenueCategory = ParseCategoryField(vla.Venue.Category)
        }).ToList();

        var response = new PublicAdvertisementDetailResponse
        {
            AdvertisementId = ad.Id,
            Title = ad.Title ?? string.Empty,
            Content = ad.Content,
            BannerUrl = ad.BannerUrl ?? string.Empty,
            TargetUrl = ad.TargetUrl,
            PlacementType = ad.PlacementType ?? string.Empty,
            Venues = venues
        };

        _logger.LogInformation("Retrieved advertisement detail with {Count} venue(s)", venues.Count);

        return response;
    }

    public async Task<SpecialEventDetailResponse> GetSpecialEventDetailAsync(int specialEventId)
    {
        _logger.LogInformation("Getting special event detail for ID {EventId}", specialEventId);

        var specialEvent = await _unitOfWork.Context.Set<SpecialEvent>()
            .Where(se => se.Id == specialEventId && se.IsDeleted != true)
            .FirstOrDefaultAsync();

        if (specialEvent == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sự kiện đặc biệt với ID {specialEventId}");
        }

        var response = new SpecialEventDetailResponse
        {
            Id = specialEvent.Id,
            EventName = specialEvent.EventName ?? string.Empty,
            Description = specialEvent.Description,
            StartDate = specialEvent.StartDate,
            EndDate = specialEvent.EndDate,
            BannerUrl = specialEvent.BannerUrl,
            IsYearly = specialEvent.IsYearly
        };

        _logger.LogInformation("Retrieved special event detail: '{EventName}'", specialEvent.EventName);

        return response;
    }

    public async Task<List<MyAdvertisementResponse>> GetPendingAdvertisementsAsync()
    {
        var advertisements = await _unitOfWork.Context.Set<Advertisement>()
            .Include(ad => ad.VenueLocationAdvertisements)
                .ThenInclude(vla => vla.Venue)
            .Where(ad => ad.Status == AdvertisementStatus.PENDING.ToString() && ad.IsDeleted != true)
            .OrderBy(ad => ad.CreatedAt)
            .ToListAsync();

        var responses = advertisements.Select(ad =>
        {
            var activeVenueAd = ad.VenueLocationAdvertisements
                .Where(vla => vla.Status == VenueLocationAdvertisementStatus.ACTIVE.ToString() && vla.EndDate >= DateTime.UtcNow)
                .OrderByDescending(vla => vla.StartDate)
                .FirstOrDefault();

            return new MyAdvertisementResponse
            {
                Id = ad.Id,
                Title = ad.Title ?? string.Empty,
                BannerUrl = ad.BannerUrl ?? string.Empty,
                PlacementType = ad.PlacementType ?? string.Empty,
                Status = ad.Status ?? AdvertisementStatus.PENDING.ToString(),
                RejectionHistory = ParseRejectionHistory(ad.RejectionReason),
                DesiredStartDate = ad.DesiredStartDate,
                CreatedAt = ad.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = ad.UpdatedAt,
                VenueLocationCount = ad.VenueLocationAdvertisements.Count,
                ActiveVenueAd = activeVenueAd != null ? new ActiveVenueLocationAd
                {
                    Id = activeVenueAd.Id,
                    VenueId = activeVenueAd.VenueId,
                    VenueName = activeVenueAd.Venue?.Name ?? "Unknown",
                    StartDate = activeVenueAd.StartDate,
                    EndDate = activeVenueAd.EndDate,
                    PriorityScore = activeVenueAd.PriorityScore
                } : null
            };
        }).ToList();

        return responses;
    }

    private static List<string> ParseImageField(string? imageField)
    {
        if (string.IsNullOrWhiteSpace(imageField))
            return new List<string>();

        var trimmed = imageField.Trim();

        // Remove outer quotes if present (handles both ' and ")
        while ((trimmed.StartsWith("'") && trimmed.EndsWith("'")) ||
               (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")))        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();
        }

        // Check if it's a JSON array
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            try
            {
                // Unescape any escaped quotes before deserializing
                var unescaped = trimmed.Replace("\\\"", "\"");
                
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(unescaped);
                if (parsed != null && parsed.Any())
                {
                    return parsed.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                }
                return new List<string>();
            }
            catch
            {
                // If parsing fails, treat as single URL
            }
        }

        // Single URL string
        return new List<string> { trimmed };
    }

    private static List<string> ParseCategoryField(string? categoryField)
    {
        if (string.IsNullOrWhiteSpace(categoryField))
            return new List<string>();

        var trimmed = categoryField.Trim();

        // Remove outer quotes if present (handles both ' and ")
        while ((trimmed.StartsWith("'") && trimmed.EndsWith("'")) ||
               (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")))        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();
        }

        // Check if it's a JSON array
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            try
            {
                // Unescape any escaped quotes before deserializing
                var unescaped = trimmed.Replace("\\\"", "\"");
                
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(unescaped);
                if (parsed != null && parsed.Any())
                {
                    return parsed.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                }
                return new List<string>();
            }
            catch
            {
                // If parsing fails, return empty
                return new List<string>();
            }
        }

        // Single category string
        return new List<string> { trimmed };
    }

    public async Task<List<AdsOrderResponse>> GetMyAdsOrdersAsync(int userId, string? status = null)
    {
        _logger.LogInformation("Getting ads orders for user {UserId} with status filter: {Status}", userId, status ?? "all");

        // Find VenueOwnerProfile from userId
        var venueOwnerProfile = await _unitOfWork.Context.Set<VenueOwnerProfile>()
            .FirstOrDefaultAsync(vop => vop.UserId == userId && vop.IsDeleted != true);

        if (venueOwnerProfile == null)
        {
            _logger.LogWarning("User {UserId} does not have a venue owner profile", userId);
            return new List<AdsOrderResponse>();
        }

        // Query directly from AdsOrder table
        var query = _unitOfWork.Context.Set<AdsOrder>()
            .Include(ao => ao.Package)
            .Include(ao => ao.Advertisement)
                .ThenInclude(ad => ad.VenueLocationAdvertisements)
                    .ThenInclude(vla => vla.Venue)
            .Where(ao => ao.Advertisement.VenueOwnerId == venueOwnerProfile.Id 
                      && ao.Advertisement.IsDeleted != true)
            .AsQueryable();

        // Filter by ads order status if provided
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpper();
            query = query.Where(ao => ao.Status == normalizedStatus);
        }

        var adsOrders = await query
            .OrderByDescending(ao => ao.CreatedAt)
            .ToListAsync();

        var responses = adsOrders.Select(ao =>
        {
            return new AdsOrderResponse
            {
                Id = ao.Id,
                Status = ao.Status ?? "PENDING",
                CreatedAt = ao.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = ao.UpdatedAt,
                Payment = new PaymentInfo
                {
                    TransactionId = ao.Id,
                    Amount = ao.PricePaid ?? 0,
                    PaymentStatus = ao.Status ?? "PENDING",
                    PaymentMethod = "VIETQR",
                    PaidAt = ao.UpdatedAt,
                    TransactionCode = null
                },
                Package = ao.Package != null ? new PackageInfo
                {
                    Id = ao.Package.Id,
                    Name = ao.Package.Name ?? "Unknown Package",
                    Price = ao.Package.Price,
                    DurationDays = ao.Package.DurationDays,
                    PlacementType = ao.Package.Placement ?? string.Empty
                } : null,
                Advertisement = ao.Advertisement != null ? new AdvertisementInfo
                {
                    Id = ao.Advertisement.Id,
                    Title = ao.Advertisement.Title ?? string.Empty,
                    Content = ao.Advertisement.Content,
                    BannerUrl = ao.Advertisement.BannerUrl ?? string.Empty,
                    TargetUrl = ao.Advertisement.TargetUrl,
                    PlacementType = ao.Advertisement.PlacementType ?? string.Empty,
                    Status = ao.Advertisement.Status ?? string.Empty,
                    DesiredStartDate = ao.Advertisement.DesiredStartDate
                } : null,
                VenueLocationAds = ao.Advertisement?.VenueLocationAdvertisements?
                    .Select(vla => new VenueLocationAdInfo
                    {
                        Id = vla.Id,
                        VenueId = vla.VenueId,
                        VenueName = vla.Venue?.Name ?? "Unknown",
                        StartDate = vla.StartDate,
                        EndDate = vla.EndDate,
                        PriorityScore = vla.PriorityScore,
                        Status = vla.Status ?? string.Empty
                    }).ToList()
            };
        }).ToList();

        return responses;
    }

    #endregion
}
