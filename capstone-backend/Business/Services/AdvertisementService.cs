using capstone_backend.Business.DTOs.Advertisement;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class AdvertisementService : IAdvertisementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdvertisementService> _logger;
    private readonly SepayService _sepayService;
    private static int _rotationIndex = 0;
    private static readonly object _lock = new object();

    public AdvertisementService(
        IUnitOfWork unitOfWork, 
        ILogger<AdvertisementService> logger,
        SepayService sepayService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _sepayService = sepayService;
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

        // Lấy special events active
        var specialEvents = await _unitOfWork.SpecialEvents.GetActiveSpecialEventsAsync();

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
                        });
                    }

                    // Tăng rotation index cho lần gọi tiếp theo
                    _rotationIndex++;
                }
            }
        }

        // Trộn special events vào (priority thấp hơn, đặt ở cuối)
        var specialEventResponses = specialEvents.Select(se => new AdvertisementResponse
        {
            Type = "SPECIAL_EVENT",
            AdvertisementId = null,
            VenueId = null,
            SpecialEventId = se.Id,
            BannerUrl = se.BannerUrl,
        }).ToList();

        // Kết hợp: Quảng cáo trước (priority cao), special events sau
        var result = rotatedAds.Concat(specialEventResponses).ToList();

        _logger.LogInformation(
            "Retrieved {TotalCount} items: {AdCount} advertisement(s) + {SpecialEventCount} special event(s) (PlacementType: {PlacementType}, RotationIndex: {RotationIndex})",
            result.Count, rotatedAds.Count, specialEventResponses.Count, placementType ?? "all", _rotationIndex);

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

        // Create advertisement (venue will be assigned when submitting payment)
        var advertisement = new Advertisement
        {
            VenueOwnerId = venueOwnerProfile.Id,
            Title = request.Title,
            Content = request.Content,
            BannerUrl = request.BannerUrl,
            TargetUrl = request.TargetUrl,
            PlacementType = request.PlacementType,
            Status = "DRAFT", // Default to DRAFT, will be updated to PENDING after payment
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Advertisements.AddAsync(advertisement);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created advertisement ID {AdId} for venue owner {VenueOwnerId}", 
            advertisement.Id, venueOwnerProfile.Id);

        // Load with details for response
        var created = await _unitOfWork.Advertisements.GetByIdWithDetailsAsync(advertisement.Id);
        
        return MapToDetailResponse(created!);
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
                .Where(vla => vla.Status == "ACTIVE" && vla.EndDate >= DateTime.UtcNow)
                .OrderByDescending(vla => vla.StartDate)
                .FirstOrDefault();

            return new MyAdvertisementResponse
            {
                Id = ad.Id,
                Title = ad.Title ?? string.Empty,
                BannerUrl = ad.BannerUrl ?? string.Empty,
                PlacementType = ad.PlacementType ?? string.Empty,
                Status = ad.Status ?? "DRAFT",
                RejectionReason = ad.RejectionReason,
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

        // 3. Validate status
        if (advertisement.Status != "DRAFT")
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = $"Advertisement status is {advertisement.Status}, cannot submit."
            };
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
                && ao.Status == "PENDING"
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

        // 7. Validate venue from request
        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(request.VenueId);
        if (venue == null || venue.IsDeleted == true)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = $"Venue with ID {request.VenueId} not found"
            };
        }

        if (venue.VenueOwnerId != venueOwnerProfile.Id)
        {
            return new SubmitAdvertisementWithPaymentResponse
            {
                IsSuccess = false,
                Message = "You can only create advertisements for your own venues"
            };
        }

        // 8. Calculate amount
        var totalAmount = package.Price;

        // 9. Start transaction
        using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync();

        try
        {
            // 10. Create AdsOrder (PENDING)
            var adsOrder = new AdsOrder
            {
                PackageId = request.PackageId,
                AdvertisementId = advertisementId,
                PricePaid = null, // Will be set after payment confirmation
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Context.Set<AdsOrder>().AddAsync(adsOrder);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Created AdsOrder ID: {OrderId}", adsOrder.Id);

            // 10.5 Create VenueLocationAdvertisement  
            var venueLocationAd = new VenueLocationAdvertisement
            {
                AdvertisementId = advertisementId,
                VenueId = request.VenueId,
                PriorityScore = package.PriorityScore, // Set from package
                StartDate = DateTime.UtcNow, // Will be updated when payment is confirmed
                EndDate = DateTime.UtcNow.AddDays(package.DurationDays), // Will be updated when payment is confirmed
                Status = "PENDING_PAYMENT",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Context.Set<VenueLocationAdvertisement>().AddAsync(venueLocationAd);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Created VenueLocationAdvertisement ID: {VlaId} for VenueId: {VenueId}", 
                venueLocationAd.Id, request.VenueId);

            // 11. Create Transaction
            var paymentContent = $"ADO{adsOrder.Id}";

            var transaction = new Transaction
            {
                UserId = userId,
                Amount = totalAmount,
                Currency = "VND",
                PaymentMethod = "VIETQR",
                TransType = 2, // ADS_ORDER
                DocNo = adsOrder.Id,
                Description = $"Thanh toán quảng cáo {package.Name} cho {venue.Name}",
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Context.Set<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Created transaction ID: {TxId} for VenueId: {VenueId}", transaction.Id, request.VenueId);

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
                venueId = request.VenueId, // Store venueId for webhook processing
                expireAt,
                bankInfo = new { bankName, accountNumber, accountName }
            });

            transaction.ExternalRefCode = externalRef;
            transaction.UpdatedAt = DateTime.UtcNow;
            
            _unitOfWork.Context.Set<Transaction>().Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            // 14. Commit transaction
            await dbTransaction.CommitAsync();

            _logger.LogInformation("✅ Payment initiated - TxId: {TxId}, AdsOrderId: {AdsOrderId}, SepayId: {SepayId}, VenueId: {VenueId}", 
                transaction.Id, adsOrder.Id, sepayResponse.Data.Id, request.VenueId);

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
                DurationDays = package.DurationDays
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

    public async Task<List<AdvertisementPackageResponse>> GetAdvertisementPackagesAsync()
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

        _logger.LogInformation("Found {Count} active advertisement packages", responses.Count);

        return responses;
    }

    #region Helper Methods

    private AdvertisementDetailResponse MapToDetailResponse(Advertisement ad)
    {
        return new AdvertisementDetailResponse
        {
            Id = ad.Id,
            VenueOwnerId = ad.VenueOwnerId,
            Title = ad.Title ?? string.Empty,
            Content = ad.Content,
            BannerUrl = ad.BannerUrl ?? string.Empty,
            TargetUrl = ad.TargetUrl,
            PlacementType = ad.PlacementType ?? string.Empty,
            Status = ad.Status ?? "DRAFT",
            RejectionReason = ad.RejectionReason,
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

    #endregion
}
