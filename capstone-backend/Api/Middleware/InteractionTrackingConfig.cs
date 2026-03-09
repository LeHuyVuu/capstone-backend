namespace capstone_backend.Api.Middleware
{
    public class InteractionTrackingRule
    {
        public string Method { get; set; } = "*";

        public string RoutePattern { get; set; } = string.Empty;

        public string InteractionType { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;

        public string TargetIdParameter { get; set; } = "id";
        public bool Enabled { get; set; } = true;
    }

    public static class InteractionTrackingConfiguration
    {
        /// <summary>
        /// Centralized configuration for auto-tracked endpoints
        /// ⚠️ CHỈ CONFIG CÁC ENDPOINTS THỰC TẾ ĐÃ CÓ TRONG CODE!
        /// Kiểm tra Controllers trước khi thêm rule mới.
        /// </summary>
        public static List<InteractionTrackingRule> TrackingRules = new()
        {
            // ==========================================
            // VENUE LOCATION (VenueLocationController)
            // ==========================================
            new InteractionTrackingRule
            {
                Method = "GET",
                RoutePattern = "/api/VenueLocation/{id}",
                InteractionType = "VIEW",
                TargetType = "VenueLocation",
                TargetIdParameter = "id",
                Enabled = true
            },

            // ==========================================
            // ADVERTISEMENT (AdvertisementController)
            // ==========================================
            new InteractionTrackingRule
            {
                Method = "GET",
                RoutePattern = "/api/Advertisement/{id}",
                InteractionType = "VIEW",
                TargetType = "Advertisement",
                TargetIdParameter = "id",
                Enabled = true
            },
            new InteractionTrackingRule
            {
                Method = "GET",
                RoutePattern = "/api/Advertisement/detail/{id}",
                InteractionType = "VIEW",
                TargetType = "Advertisement",
                TargetIdParameter = "id",
                Enabled = true
            },

            // ==========================================
            // CHALLENGE (CoupleProfileChallengeController)
            // ==========================================
            new InteractionTrackingRule
            {
                Method = "GET",
                RoutePattern = "/api/CoupleProfileChallenge/challenges/{challengeId}",
                InteractionType = "VIEW",
                TargetType = "Challenge",
                TargetIdParameter = "challengeId",
                Enabled = true
            },
            new InteractionTrackingRule
            {
                Method = "POST",
                RoutePattern = "/api/CoupleProfileChallenge/challenges/{challengeId}/join",
                InteractionType = "APPLY",
                TargetType = "Challenge",
                TargetIdParameter = "challengeId",
                Enabled = true
            },

            // ==========================================
            // COLLECTION (CollectionController)
            // ==========================================
            new InteractionTrackingRule
            {
                Method = "GET",
                RoutePattern = "/api/Collection/{id}",
                InteractionType = "VIEW",
                TargetType = "Collection",
                TargetIdParameter = "id",
                Enabled = true
            },
            new InteractionTrackingRule
            {
                Method = "POST",
                RoutePattern = "/api/Collection/{id}/venue/{venueId}",
                InteractionType = "SAVE",
                TargetType = "VenueLocation",
                TargetIdParameter = "venueId",
                Enabled = true
            }

            // ==========================================
            // 💡 MUỐN TRACK FAVORITE/SHARE/CLICK?
            // ==========================================
            // BƯỚC 1: TẠO ENDPOINT trong controller trước!
            // Example - Thêm vào VenueLocationController.cs:
            //
            // [HttpPost("{id}/favorite")]
            // public async Task<IActionResult> FavoriteVenue(int id)
            // {
            //     await _service.AddToFavoriteAsync(id);
            //     return OkResponse("Favorited");
            // }
            //
            // BƯỚC 2: SAU ĐÓ mới thêm rule vào đây:
            //
            // new InteractionTrackingRule
            // {
            //     Method = "POST",
            //     RoutePattern = "/api/VenueLocation/{id}/favorite",
            //     InteractionType = "FAVORITE",
            //     TargetType = "VenueLocation",
            //     TargetIdParameter = "id"
            // }
        };

        /// <summary>
        /// Add a new tracking rule dynamically
        /// </summary>
        public static void AddRule(InteractionTrackingRule rule)
        {
            TrackingRules.Add(rule);
        }

        /// <summary>
        /// Remove tracking for a specific route pattern
        /// </summary>
        public static void RemoveRule(string routePattern)
        {
            TrackingRules.RemoveAll(r => r.RoutePattern.Equals(routePattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Enable/disable tracking for a specific route
        /// </summary>
        public static void SetRuleEnabled(string routePattern, bool enabled)
        {
            var rule = TrackingRules.FirstOrDefault(r => 
                r.RoutePattern.Equals(routePattern, StringComparison.OrdinalIgnoreCase));
            
            if (rule != null)
            {
                rule.Enabled = enabled;
            }
        }
    }
}
