﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebMatrix.Data;
using System.Web.WebPages;

public static partial class LcData
{
    public class SqlGenericResult
    {
        public int Error = 0;
        public string ErrorMessage = "";
        public int ID = 0;
        public SqlGenericResult() { }
        public SqlGenericResult(dynamic record)
        {
            Error = record.Error;
            try
            {
                ErrorMessage = record.ErrorMessage;
            } catch {}
            try
            {
                ID = record.ID;
            } catch {}
        }
    }

    /// <summary>
    /// Methods to get data related with Bookings
    /// </summary>
    public static partial class Booking
    {
        /// <summary>
        /// After that time from the last update on a
        /// booking request without provider confirmation,
        /// the request will expire.
        /// </summary>
        public const int ConfirmationLimitInHours = 18;

        #region Query list of bookings
        #region SQLs
        private const string sqlGetBookingsByDateRange = @"
        SELECT  R.BookingRequestID,
                B.BookingID,
                R.ProviderUserID,
                R.CustomerUserID,
                R.PricingEstimateID,
                R.BookingRequestStatusID,
                B.BookingStatusID,

                UC.FirstName As CustomerFirstName,
                UC.LastName As CustomerLastName,

                UP.FirstName As ProviderFirstName,
                UP.LastName As ProviderLastName,

                E.StartTime,
                E.EndTime,
                E.TimeZone,
                Pos.PositionSingular,
                Pr.TotalPrice As CustomerPrice,
                (Pr.SubtotalPrice - Pr.PFeePrice) As ProviderPrice
        FROM    Booking As B
                 INNER JOIN
                BookingRequest As R
                  ON B.BookingRequestID = R.BookingRequestID
                 INNER JOIN
                PricingEstimate As Pr
                  ON Pr.PricingEstimateID = R.PricingEstimateID
                 INNER JOIN
                Users As UC
                  ON UC.UserID = R.CustomerUserID
                 INNER JOIN
                Users As UP
                  ON UP.UserID = R.ProviderUserID
                 LEFT JOIN
                CalendarEvents As E
                  ON E.Id = B.ConfirmedDateID
                 INNER JOIN
                Positions As Pos
                  ON Pos.PositionID = R.PositionID
					AND Pos.LanguageID = @3 AND Pos.CountryID = @4
        WHERE   (
                 R.CustomerUserID=@0
                  OR
                 R.ProviderUserID=@0
                )
                 AND
                (   @1 is null AND E.StartTime is null
                    OR
                    Convert(date, E.StartTime) >= @1
                     AND
                    Convert(date, E.StartTime) <= @2
                )
        ORDER BY E.StartTime DESC, B.UpdatedDate DESC, R.UpdatedDate DESC
        ";
        private const string sqlGetBookingsSumByDateRange = @"
            SELECT  count(*) as count,
                    min(E.StartTime) as startTime,
                    max(E.EndTime) as endTime
            FROM    Booking As B
                     INNER JOIN
                    BookingRequest As R
                      ON B.BookingRequestID = R.BookingRequestID
                     INNER JOIN
                    CalendarEvents As E
                      ON E.Id = B.ConfirmedDateID
            WHERE   
                    -- Any bookings, as provider or customer
                    (
                     R.CustomerUserID=@0
                      OR
                     R.ProviderUserID=@0
                    )
                     AND
                    (   
                        E.StartTime >= @1
                         AND
                        E.StartTime <= @2
                    )
        ";
        private const string sqlGetNextBookingID = @"
            SELECT  TOP 1
                    B.BookingID
            FROM    Booking As B
                     INNER JOIN
                    BookingRequest As R
                      ON B.BookingRequestID = R.BookingRequestID
                     INNER JOIN
                    CalendarEvents As E
                      ON E.Id = B.ConfirmedDateID
            WHERE   
                    -- Any bookings, as provider or customer
                    (
                     R.CustomerUserID=@0
                      OR
                     R.ProviderUserID=@0
                    )
                     AND
                    (   
                        E.StartTime >= @1
                    )
            ORDER BY E.StartTime ASC
        ";
        #endregion

        public static Dictionary<string, dynamic> GetUpcomingBookings(int userID, int upcomingLimit = 10) {
            var ret = new Dictionary<string, dynamic>();
            
            // Preparing dates for further filtering
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            // This week must not include today and tomorrow, to avoid duplicated entries
            var upcomingFirstDay = tomorrow.AddDays(1);
            var upcomingLastDay = System.Data.SqlTypes.SqlDateTime.MaxValue; // DateTime.MaxValue;

            using (var db = Database.Open("sqlloco"))
            {
                var l = LcData.GetCurrentLanguageID();
                var c = LcData.GetCurrentCountryID();
                dynamic d = null;

                d = db.Query(sqlGetBookingsByDateRange, userID, today, today, l, c);
                if (d != null && d.Count > 0)
                {
                    ret["today"] = d;
                }
                d = db.Query(sqlGetBookingsByDateRange, userID, tomorrow, tomorrow, l, c);
                if (d != null && d.Count > 0)
                {
                    ret["tomorrow"] = d;
                }

                var sqlLimited = sqlGetBookingsByDateRange.Replace("SELECT", "SELECT TOP " + upcomingLimit.ToString());
                d = db.Query(sqlLimited, userID, upcomingFirstDay, upcomingLastDay, l, c);
                if (d != null && d.Count > 0)
                {
                    ret["upcoming"] = d;
                }
            }

            return ret;
        }

        public class RestUpcomingBookingsInfo
        {
            public int quantity;
            public DateTime? time;
        }

        public static Dictionary<string, dynamic> GetUpcomingBookingsInfo(int userID)
        {
            var ret = new Dictionary<string, dynamic>();
            
            // Preparing dates for further filtering
            var leftToday = DateTime.Now;
            var tomorrow = DateTime.Today.AddDays(1);
            // Next week is from tomorrow up to 7 days
            var nextWeekStart = tomorrow;
            var nextWeekEnd = nextWeekStart.AddDays(7);

            using (var db = Database.Open("sqlloco"))
            {
                dynamic d = null;

                d = db.QuerySingle(sqlGetBookingsSumByDateRange, userID, leftToday, tomorrow);
                ret["today"] = new RestUpcomingBookingsInfo {
                    quantity = d.count,
                    // NOTE: What if the endTime is for a different date? Last work hour on the date?
                    time = d.endTime
                };
                
                // NOTE: what if there is a booking for several days and we are in the middel of that? First work hour on the date?
                d = db.QuerySingle(sqlGetBookingsSumByDateRange, userID, tomorrow, tomorrow.AddDays(1).AddSeconds(-1));
                ret["tomorrow"] = new RestUpcomingBookingsInfo {
                    quantity = d.count,
                    time = d.startTime
                };

                d = db.QuerySingle(sqlGetBookingsSumByDateRange, userID, nextWeekStart, nextWeekEnd);
                ret["nextWeek"] = new RestUpcomingBookingsInfo {
                    quantity = d.count,
                    time = d.startTime
                };

                ret["nextBookingID"] = db.QueryValue(sqlGetNextBookingID, userID, leftToday);
            }

            return ret;
        }
        #endregion

        #region Query Booking information
        #region SQLs
        public const string sqlGetBookingRequestPricingEstimate = @"
            SELECT  PricingEstimateID
            FROM    BookingRequest
            WHERE   BookingRequestID = @0
        ";
        public const string sqlGetPricingPackagesInPricingEstimate = @"
            SELECT  PP.ProviderPackageID
                    ,PP.ProviderUserID
                    ,PP.PricingTypeID
                    ,PP.PositionID
                    ,PP.ProviderPackageName As Name
                    ,PP.ProviderPackageDescription As Description

                    --,PP.ProviderPackagePrice As Price
                    ,P.FirstSessionDuration As ServiceDuration
                    ,P.ServiceDuration As AllSessionsServiceDuration
                    ,P.TotalPrice As Price

                    ,PP.FirstTimeClientsOnly
                    ,PP.NumberOfSessions
                    ,PP.PriceRate
                    ,PP.PriceRateUnit
                    ,PP.IsPhone
                    ,P.CustomerPricingDataInput
                    ,PP.LanguageID
                    ,PP.CountryID
                    ,PP.Active
            FROM    PricingEstimateDetail As P
                     INNER JOIN
                    ProviderPackage As PP
                      ON PP.ProviderPackageID = P.ProviderPackageID
                     INNER JOIN
                    PricingType As PT
                        ON PP.PricingTypeID = PT.PricingTypeID
                        AND PP.LanguageID = PT.LanguageID
                        AND PP.CountryID = PT.CountryID
            WHERE   P.PricingEstimateID = @0
                     AND 
                    P.PricingEstimateRevision = @1
                     AND
                    PP.LanguageID = @2 AND PP.CountryID = @3
            ORDER BY PT.DisplayRank
        ";
        #endregion

        /// <summary>
        /// Get a dynamic record with the most basic info from 
        /// a Booking: CustomerUserID, ProviderUserID, PositionID,
        /// BookingID, BookingStatusID, BookingRequestID
        /// </summary>
        /// <param name="BookingID"></param>
        /// <returns></returns>
        public static dynamic GetBookingBasicInfo(int BookingID)
        {
            var sqlGetBooking = @"
                SELECT  R.CustomerUserID, R.ProviderUserID,
                        R.PositionID, B.BookingID, B.BookingStatusID,
                        R.BookingRequestID
                FROM    Booking As B
                         INNER JOIN
                        BookingRequest As R
                          ON R.BookingRequestID = B.BookingRequestID
                WHERE   BookingID = @0
            ";
            using (var db = Database.Open("sqlloco"))
            {
                return db.QuerySingle(sqlGetBooking, BookingID);
            }
        }
        /// <summary>
        /// Get a dynamic record with the most basic information
        /// about a Booking Request: customer and provider IDs, positionID,
        /// BookingRequestStatusID.
        /// </summary>
        /// <param name="BookingRequestID"></param>
        /// <returns></returns>
        public static dynamic GetBookingRequestBasicInfo(int BookingRequestID)
        {
            var sqlGetBookingRequest = @"
                SELECT  R.CustomerUserID, R.ProviderUserID,
                        R.PositionID,
                        R.BookingRequestID,
                        R.BookingRequestStatusID
                FROM    BookingRequest As R
                WHERE   BookingRequestID = @0
            ";
            using (var db = Database.Open("sqlloco"))
            {
                return db.QuerySingle(sqlGetBookingRequest, BookingRequestID);
            }
        }
        public static dynamic GetBooking(int BookingID)
        {
            var sqlGetBooking = @"
                DECLARE @PeRevision int
                SELECT  TOP 1 @PeRevision = PricingEstimateRevision
                FROM    PricingEstimate
                WHERE   PricingEstimateID = (
                        SELECT  PricingEstimateID
                        FROM    BookingRequest
                                 INNER JOIN
                                Booking
                                  ON BookingRequest.BookingRequestID = Booking.BookingRequestID
                        WHERE   Booking.BookingID = @0
                    )
                ORDER BY PricingEstimateRevision DESC

                SELECT  R.BookingRequestID,
                        B.BookingID,
                        R.ProviderUserID,
                        R.CustomerUserID,
                        R.PositionID,
                        R.PricingEstimateID,
                        Pr.PricingEstimateRevision,
                        R.BookingRequestStatusID,
                        B.BookingStatusID,
                        R.InstantBooking,

                        UC.FirstName As CustomerFirstName,
                        UC.LastName As CustomerLastName,

                        UP.FirstName As ProviderFirstName,
                        UP.LastName As ProviderLastName,

                        DATEADD(day, 7, E.EndTime) As PaymentDate,
                        (SELECT TOP 1 LastThreeAccountDigits FROM ProviderPaymentPreference
                         WHERE ProviderPaymentPreference.ProviderUserID = R.ProviderUserID)
                         As PaymentProviderAccountLastDigits,
                        R.PaymentLastFourCardNumberDigits As PaymentCustomerCardLastDigits,

                        E.StartTime,
                        E.EndTime,
                        E.TimeZone,
                        Pos.PositionSingular,
                        Pr.TotalPrice,
                        Pr.FeePrice,
                        Pr.ServiceDuration,
                    
                        CAST(CASE WHEN (SELECT count(*) FROM UserReviews As URP
                         WHERE URP.BookingID = B.BookingID
                                AND
                               URP.ProviderUserID = R.ProviderUserID
                                AND 
                               URP.PositionID = 0
                        ) = 0 THEN 0 ELSE 1 END As bit) As ReviewedByProvider,
                        CAST(CASE WHEN (SELECT count(*) FROM UserReviews As URC
                         WHERE URC.BookingID = B.BookingID
                                AND
                               URC.CustomerUserID = R.CustomerUserID
                                AND 
                               URC.PositionID = R.PositionID
                        ) = 0 THEN 0 ELSE 1 END As bit) As ReviewedByCustomer
                FROM    Booking As B
                         INNER JOIN
                        BookingRequest As R
                          ON B.BookingRequestID = R.BookingRequestID
                         INNER JOIN
                        PricingEstimate As Pr
                          ON Pr.PricingEstimateID = R.PricingEstimateID
                            AND Pr.PricingEstimateRevision = @PeRevision
                         INNER JOIN
                        Users As UC
                          ON UC.UserID = R.CustomerUserID
                         INNER JOIN
                        Users As UP
                          ON UP.UserID = R.ProviderUserID
                         LEFT JOIN
                        CalendarEvents As E
                          ON E.Id = B.ConfirmedDateID
                         INNER JOIN
                        Positions As Pos
                          ON Pos.PositionID = R.PositionID
					        AND Pos.LanguageID = @1 AND Pos.CountryID = @2
                WHERE   B.BookingID = @0
            ";
            using (var db = Database.Open("sqlloco"))
            {
                return db.QuerySingle(sqlGetBooking, BookingID, LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID());
            }
        }
        public static dynamic GetBookingForUser(int BookingID, int UserID, bool IsAdmin)
        {
            var sqlGetBooking = @"
                DECLARE @PeRevision int
                SELECT  TOP 1 @PeRevision = PricingEstimateRevision
                FROM    PricingEstimate
                WHERE   PricingEstimateID = (
                        SELECT  PricingEstimateID
                        FROM    BookingRequest
                                 INNER JOIN
                                Booking
                                  ON BookingRequest.BookingRequestID = Booking.BookingRequestID
                        WHERE   Booking.BookingID = @0
                    )
                ORDER BY PricingEstimateRevision DESC

                SELECT  R.BookingRequestID,
                        B.BookingID,
                        R.ProviderUserID,
                        R.CustomerUserID,
                        R.PricingEstimateID,
                        P.PricingEstimateRevision,
                        R.SpecialRequests,
                        R.CreatedDate As RequestDate,
                        B.CreatedDate As BookingDate,
                        R.UpdatedDate,
                        R.BookingRequestStatusID,
                        B.BookingStatusID,
                        R.PositionID,
                        R.CancellationPolicyID,
                        R.InstantBooking,

                        DATEADD(day, 7, E.EndTime) As PaymentDate,
                        (SELECT TOP 1 LastThreeAccountDigits FROM ProviderPaymentPreference
                         WHERE ProviderPaymentPreference.ProviderUserID = R.ProviderUserID)
                         As PaymentProviderAccountLastDigits,
                        R.PaymentLastFourCardNumberDigits As PaymentCustomerCardLastDigits,

                        Pos.PositionSingular,

                        L.UserID As LocationUserID,
                        L.AddressName As LocationName,
                        L.AddressLine1, L.AddressLine2,
                        L.City,
                        SP.StateProvinceName, SP.StateProvinceCode,
                        PC.PostalCode,
                        L.CountryID,
                        L.SpecialInstructions As LocationSpecialInstructions,

                        E.StartTime As ConfirmedDateStart, E.EndTime As ConfirmedDateEnd,

                        coalesce(P.ServiceDuration, 0) As ServiceDuration,
                        coalesce(P.SubtotalPrice, 0) As SubtotalPrice,
                        coalesce(P.FeePrice, 0) As FeePrice,
                        coalesce(P.TotalPrice, 0) As TotalPrice,
                        coalesce(P.PFeePrice, 0) As PFeePrice,
                        coalesce(P.SubtotalRefunded, 0) As SubtotalRefunded,
                        coalesce(P.FeeRefunded, 0) As FeeRefunded,
                        coalesce(P.TotalRefunded, 0) As TotalRefunded,
                        P.DateRefunded As DateRefunded,

                        CAST(CASE WHEN (SELECT count(*) FROM UserReviews As URP
                            WHERE URP.BookingID = B.BookingID
                                AND
                                URP.ProviderUserID = R.ProviderUserID
                                AND 
                                URP.PositionID = 0
                        ) = 0 THEN 0 ELSE 1 END As bit) As ReviewedByProvider,
                        CAST(CASE WHEN (SELECT count(*) FROM UserReviews As URC
                            WHERE URC.BookingID = B.BookingID
                                AND
                                URC.CustomerUserID = R.CustomerUserID
                                AND 
                                URC.PositionID = R.PositionID
                        ) = 0 THEN 0 ELSE 1 END As bit) As ReviewedByCustomer
                FROM    BookingRequest As R
                         INNER JOIN
                        Booking As B
                          ON R.BookingRequestID = B.BookingRequestID
                         INNER JOIN
                        PricingEstimate As P
                          ON P.PricingEstimateID = R.PricingEstimateID
                            AND P.PricingEstimateRevision = @PeRevision
                         LEFT JOIN
                        Address As L
                          ON R.AddressID = L.AddressID
                         LEFT JOIN
                        ServiceAddress As SA
                          ON L.AddressID = SA.AddressID
                         LEFT JOIN
                        StateProvince As SP
                          ON SP.StateProvinceID = L.StateProvinceID
                         LEFT JOIN
                        PostalCode As PC
                          ON PC.PostalCodeID = L.PostalCodeID
                         LEFT JOIN
                        CalendarEvents As E
                          ON E.ID = B.ConfirmedDateID
                         INNER JOIN
                        Positions As Pos
                          ON Pos.PositionID = R.PositionID
					        AND Pos.LanguageID = @2 AND Pos.CountryID = @3
                WHERE   B.BookingID = @0
                         AND
                        (R.ProviderUserID = @1 OR R.CustomerUserID = @1 OR 1=@4)
            ";
            using (var db = Database.Open("sqlloco"))
            {
                return db.QuerySingle(sqlGetBooking, BookingID, UserID,
                    LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID(),
                    IsAdmin);
            }
        }
        public static dynamic GetBookingRequestForUser(int BookingRequestID, int UserID, bool IsAdmin)
        {
            var sqlGetBookingRequest = @"
                DECLARE @PeRevision int
                SELECT  TOP 1 @PeRevision = PricingEstimateRevision
                FROM    PricingEstimate
                WHERE   PricingEstimateID = (
                        SELECT  PricingEstimateID
                        FROM    BookingRequest
                        WHERE   BookingRequestID = @0
                    )
                ORDER BY PricingEstimateRevision DESC

                SELECT  R.BookingRequestID,
                        0 As BookingID,
                        R.ProviderUserID,
                        R.CustomerUserID,
                        R.PricingEstimateID,
                        P.PricingEstimateRevision,
                        R.SpecialRequests,
                        R.CreatedDate As RequestDate,
                        R.UpdatedDate,
                        R.BookingRequestStatusID,
                        0 As BookingStatusID,
                        R.PositionID,
                        R.CancellationPolicyID,
                        R.InstantBooking,

                        DATEADD(day, 7, E1.EndTime) As PaymentDate,
                        (SELECT TOP 1 LastThreeAccountDigits FROM ProviderPaymentPreference
                         WHERE ProviderPaymentPreference.ProviderUserID = R.ProviderUserID)
                         As PaymentProviderAccountLastDigits,
                        R.PaymentLastFourCardNumberDigits As PaymentCustomerCardLastDigits,

                        Pos.PositionSingular,

                        L.UserID As LocationUserID,
                        L.AddressName As LocationName,
                        L.AddressLine1, L.AddressLine2,
                        L.City,
                        L.StateProvinceID,
                        SP.StateProvinceName, SP.StateProvinceCode,
                        L.PostalCodeID,
                        PC.PostalCode,
                        L.CountryID,
                        L.SpecialInstructions As LocationSpecialInstructions,

                        E1.StartTime As PreferredDateStart, E1.EndTime As PreferredDateEnd,
                        E2.StartTime As AlternativeDate1Start, E2.EndTime As AlternativeDate1End,
                        E3.StartTime As AlternativeDate2Start, E3.EndTime As AlternativeDate2End,

                        coalesce(P.ServiceDuration, 0) As ServiceDuration,
                        coalesce(P.SubtotalPrice, 0) As SubtotalPrice,
                        coalesce(P.FeePrice, 0) As FeePrice,
                        coalesce(P.TotalPrice, 0) As TotalPrice,
                        coalesce(P.PFeePrice, 0) As PFeePrice,
                        coalesce(P.SubtotalRefunded, 0) As SubtotalRefunded,
                        coalesce(P.FeeRefunded, 0) As FeeRefunded,
                        coalesce(P.TotalRefunded, 0) As TotalRefunded,
                        P.DateRefunded As DateRefunded
                FROM    BookingRequest As R
                         INNER JOIN
                        PricingEstimate As P
                          ON P.PricingEstimateID = R.PricingEstimateID
                            AND P.PricingEstimateRevision = @PeRevision
                         LEFT JOIN
                        Address As L
                          ON R.AddressID = L.AddressID
                        -- LEFT JOIN
                        --ServiceAddress As SA
                        --  ON L.AddressID = SA.AddressID
                         LEFT JOIN
                        CalendarEvents As E1
                          ON E1.ID = R.PreferredDateID
                         LEFT JOIN
                        CalendarEvents As E2
                          ON E2.ID = R.AlternativeDate1ID
                         LEFT JOIN
                        CalendarEvents As E3
                          ON E3.ID = R.AlternativeDate2ID
                         INNER JOIN
                        Positions As Pos
                          ON Pos.PositionID = R.PositionID
					        AND Pos.LanguageID = @2 AND Pos.CountryID = @3
                         LEFT JOIN
                        StateProvince As SP
                          ON SP.StateProvinceID = L.StateProvinceID
                         LEFT JOIN
                        PostalCode As PC
                          ON PC.PostalCodeID = L.PostalCodeID
                WHERE   R.BookingRequestID = @0
                         AND
                        (R.ProviderUserID = @1 OR R.CustomerUserID = @1 OR 1=@4)
            ";
            using (var db = Database.Open("sqlloco"))
            {
                return db.QuerySingle(sqlGetBookingRequest, BookingRequestID, UserID,
                    LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID(),
                    IsAdmin);
            }
        }
        public static int GetBookingIDForBookingRequestID(int BookingRequestID)
        {
            using (var db = Database.Open("sqlloco"))
            {
                return db.QueryValue(@"
                    SELECT BookingID FROM Booking WHERE BookingRequestID = @0
                ", BookingRequestID) ?? 0;
            }
        }

        #region Pricing Summary
        public static dynamic GetPricingSummary(dynamic summaryData)
        {
            var pricingSummary = new LcPricingModel.PricingSummaryData();
            pricingSummary.ServiceDuration = summaryData.ServiceDuration;
            pricingSummary.SubtotalPrice = summaryData.SubtotalPrice;
            pricingSummary.FeePrice = summaryData.FeePrice;
            pricingSummary.TotalPrice = summaryData.TotalPrice;
            pricingSummary.PFeePrice = summaryData.PFeePrice;
            return pricingSummary;
        }
        public static dynamic GetPricingDetailsGroups(int PricingEstimateID)
        {
            var sql = @"
                SELECT  S.*
                        ,G.InternalGroupName
                        ,G.SelectionTitle
                        ,G.SummaryTitle
                        ,G.DynamicSummaryTitle
                FROM (
                SELECT
                        P.PricingGroupID
                        ,sum(P.ServiceDuration) As ServiceDuration
                        ,sum(P.HourlyPrice) As HourlyPrice
                        ,sum(P.SubtotalPrice) As SubtotalPrice
                        ,sum(P.FeePrice) As FeePrice
                        ,sum(P.TotalPrice) As TotalPrice
                FROM
                    PricingEstimateDetail As P
                WHERE   p.PricingEstimateID = @0
                GROUP BY P.PricingGroupID
                HAVING sum(P.TotalPrice) <> 0
                ) As S
                 INNER JOIN
                PricingGroups As G
                  ON S.PricingGroupID = G.PricingGroupID
                WHERE
                    G.LanguageID = @1
                    AND G.CountryID = @2
            ";
            using (var db = Database.Open("sqlloco"))
            {
                return db.Query(sql, PricingEstimateID,
                    LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID());
            }
        }
        public static Dictionary<string, LcPricingModel.PricingSummaryData> GetPricingSummaryGroups(int PricingEstimateID, dynamic bookingData = null)
        {
            dynamic pricingGroups = GetPricingDetailsGroups(PricingEstimateID);
            var pricingSummaryGroups = new Dictionary<string, LcPricingModel.PricingSummaryData>();
            if (pricingGroups != null)
                foreach (var g in pricingGroups)
                {
                    var s = new LcPricingModel.PricingSummaryData();
                    var concept = g.SummaryTitle;
                    switch ((int)g.PricingGroupID)
                    {
                        case 4: // packages
                            concept = g.DynamicSummaryTitle.Replace("{package}", GetOneLineBookingRequestPackages(0, PricingEstimateID, false));
                            break;
                        case 5: // addons
                            concept = g.DynamicSummaryTitle.Replace("{addons}", GetOneLineBookingRequestPackages(0, PricingEstimateID, true));
                            break;
                    }
                    s.Concept = concept;
                    s.ServiceDuration = g.ServiceDuration;
                    s.SubtotalPrice = g.SubtotalPrice;
                    s.FeePrice = g.FeePrice;
                    s.TotalPrice = g.TotalPrice;
                    pricingSummaryGroups.Add(g.InternalGroupName, s);
                }
            return pricingSummaryGroups;
        }
        public static List<LcPricingModel.PricingSummaryData> GetPricingSummaryDetails(int PricingEstimateID, int PricingEstimateRevision)
        {
            var details = new List<LcPricingModel.PricingSummaryData>();
            var sql = @"
                SELECT
                        D.ServiceDuration
                        ,D.FirstSessionDuration
                        ,D.HourlyPrice
                        ,D.SubtotalPrice
                        ,D.FeePrice
                        ,D.TotalPrice
                        ,P.ProviderUserID
                        ,P.ProviderPackageName
                FROM    PricingEstimateDetail As D
                         INNER JOIN
                        ProviderPackage As P
                          ON P.ProviderPackageID = D.ProviderPackageID
                WHERE   D.PricingEstimateID = @0
                         AND
                        D.PricingEstimateRevision = @1
            ";
            using (var db = Database.Open("sqlloco"))
            {
                var data = db.Query(sql, PricingEstimateID, PricingEstimateRevision);
                foreach (var r in data)
                {
                    details.Add(new LcPricingModel.PricingSummaryData{
                        Concept = r.ProviderPackageName
                        ,ServiceDuration = r.ServiceDuration
                        ,SubtotalPrice = r.SubtotalPrice
                        ,FeePrice = r.FeePrice
                        ,TotalPrice = r.TotalPrice
                        ,FirstSessionDuration = r.FirstSessionDuration
                    });
                }
            }
            return details;
        }
        #endregion

        public static IEnumerable<LcPricingModel.PackageBaseData> GetPricingEstimatePackages(int pricingEstimateID, int pricingEstimateRevision = 0)
        {
            using (var db = Database.Open("sqlloco"))
            {
                // When 0, look for last revision
                if (pricingEstimateRevision == 0)
                {
                    pricingEstimateRevision = db.QueryValue("SELECT Max(PricingEstimateRevision) FROM PricingEstimate WHERE PricingEstimateID = @0", pricingEstimateID);
                }
                foreach (var pak in db.Query(LcData.Booking.sqlGetPricingPackagesInPricingEstimate,
                    pricingEstimateID, pricingEstimateRevision,
                    LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID()))
                {
                    var pakdata = new LcPricingModel.PackageBaseData(pak);
                    pakdata.Duration = TimeSpan.FromHours((double)pak.ServiceDuration);
                    yield return pakdata;
                }
            }
        }

        public static int GetPricingEstimateIDForBookingRequest(int bookingRequestID)
        {
            using (var db = Database.Open("sqlloco"))
            {
                return db.QueryValue(LcData.Booking.sqlGetBookingRequestPricingEstimate, bookingRequestID);
            }
        }

        public static string GetOneLineBookingRequestPackages(int bookingRequestID, int pricingEstimateID = 0, bool? addons = null)
        {
            // TODO Include Revision in parameters or/and database lookup
            int pricingEstimateRevision = 1;
            dynamic packages;
            using (var db = Database.Open("sqlloco"))
            {
                if (pricingEstimateID == 0)
                {
                    pricingEstimateID = db.QueryValue(LcData.Booking.sqlGetBookingRequestPricingEstimate, bookingRequestID);
                }
                packages = db.Query(LcData.Booking.sqlGetPricingPackagesInPricingEstimate, pricingEstimateID, 1 /* revision */,
                    LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID());
            }
            var details = new List<string>();

            foreach (var pak in packages)
            {
                // Filter add-ons: show both packages and addons if null, only addons if true and not addons if false.
                if (addons.HasValue)
                    if (addons.Value && pak.PricingTypeID != 7)
                        continue;
                    else if (!addons.Value && pak.PricingTypeID == 7)
                        continue;

                // Format for the package summary
                var pakdata = new LcPricingModel.PackageBaseData(pak);
                // This query gets duration in hours not minutes as expected by PackageBaseData constructor, fix it:
                pakdata.Duration = TimeSpan.FromHours((double)pak.ServiceDuration);

                var result = GetOneLinePackageSummary(pakdata, pricingEstimateID, pricingEstimateRevision);

                details.Add(result);
            }
            return ASP.LcHelpers.JoinNotEmptyStrings("; ", details);
        }

        /// <summary>
        /// Get the package name and main information in one line of plain-text.
        /// It shows the inperson-phone text if need, number of appointments,
        /// duration and pricing-mod extra-details following its pricing-config
        /// in a standard format for this package summary.
        /// </summary>
        /// <param name="pak">Package information, from the package itself of from a package in pricing estimate</param>
        /// <param name="pricingEstimateID">Optionally for the possible extra data associated to the package on a specific pricing</param>
        /// <param name="pricingEstimateRevision">Optionally for the possible extra data associated to the package on a specific pricing</param>
        /// <returns></returns>
        public static string GetOneLinePackageSummary(LcPricingModel.PackageBaseData pak, int pricingEstimateID = 0, int pricingEstimateRevision = 1)
        {
            var f = "";
            var inpersonphone = "";

            var pricingConfig = LcPricingModel.PackageBasePricingTypeConfigs[(int)pak.PricingTypeID];
            if (pak.NumberOfSessions > 1)
            {
                if (pak.Duration == TimeSpan.Zero)
                    f = pricingConfig.NameAndSummaryFormatMultipleSessionsNoDuration;
                else
                    f = pricingConfig.NameAndSummaryFormatMultipleSessions;
            }
            else if (pak.Duration == TimeSpan.Zero)
                f = pricingConfig.NameAndSummaryFormatNoDuration;
            if (String.IsNullOrEmpty(f))
                f = pricingConfig.NameAndSummaryFormat;

            if (pricingConfig.InPersonPhoneLabel != null)
                inpersonphone = pak.IsPhone
                    ? "phone"
                    : "in-person";

            var extraDetails = "";    
            // Extra information for special pricings:
            if (pricingConfig.Mod != null && pricingEstimateID > 0) {
                extraDetails = pricingConfig.Mod.GetPackagePricingDetails(pak.ID, pricingEstimateID, pricingEstimateRevision);
            }
                
            // Show duration in a smart way.
            var duration = ASP.LcHelpers.TimeToSmartLongString(ASP.LcHelpers.RoundTimeToMinutes(pak.Duration));

            var result = String.Format(f, pak.Name, duration, pak.NumberOfSessions, inpersonphone);
            if (!String.IsNullOrEmpty(extraDetails)) {
                result += String.Format(" ({0})", extraDetails);
            }
            return result;
        }
        public static string GetBookingRequestSubject(int BookingRequestID)
        {
            var getBookingRequest = @"
                SELECT  TOP 1
                        P.PositionSingular,
                        E1.StartTime As PreferredDateStart, E1.EndTime As PreferredDateEnd
                FROM    BookingRequest As B
                         INNER JOIN
                        CalendarEvents As E1
                          ON E1.ID = B.PreferredDateID
                         INNER JOIN
                        Positions As P
                          ON P.PositionID = B.PositionID
                WHERE   B.BookingRequestID = @0
            ";
            dynamic summary = null;
            using (var db = Database.Open("sqlloco"))
            {
                summary = db.QuerySingle(getBookingRequest, BookingRequestID);
            }
            string result = "";
            if (summary != null)
            {
                result = summary.PositionSingular + " " +
                    summary.PreferredDateStart.ToLongDateString() + ", " +
                    summary.PreferredDateStart.ToShortTimeString() + " to " +
                    summary.PreferredDateEnd.ToShortTimeString();
            }
            return result;
        }
        public static string GetBookingSubject(int BookingID)
        {
            var getBookingRequest = @"
                SELECT  TOP 1
                        P.PositionSingular,
                        E1.StartTime As ConfirmedDateStart, E1.EndTime As ConfirmedDateEnd
                FROM    Booking As B
                         INNER JOIN
                        CalendarEvents As E1
                          ON E1.ID = B.ConfirmedDateID
                         INNER JOIN
                        BookingRequest As R
                          ON R.BookingRequestID = B.BookingRequestID
                         INNER JOIN
                        Positions As P
                          ON P.PositionID = R.PositionID
                WHERE   B.BookingID = @0
            ";
            dynamic summary = null;
            using (var db = Database.Open("sqlloco"))
            {
                summary = db.QuerySingle(getBookingRequest, BookingID);
            }
            string result = "";
            if (summary != null)
            {
                result = summary.PositionSingular + " " +
                    summary.ConfirmedDateStart.ToLongDateString() + ", " +
                    summary.ConfirmedDateStart.ToShortTimeString() + " to " +
                    summary.ConfirmedDateEnd.ToShortTimeString();
            }
            return result;
        }
        public static string GetBookingStatus(int BookingID)
        {
            var getBookingStatus = @"
                SELECT  BookingStatusName, BookingStatusDescription
                FROM    BookingStatus As BS
                         INNER JOIN
                        Booking As B
                          ON B.BookingStatusID = BS.BookingStatusID
                WHERE   B.BookingID = @0
            ";
            using (var db = Database.Open("sqlloco"))
            {
                var b = db.QuerySingle(getBookingStatus, BookingID);
                if (b != null)
                {
                    return b.BookingStatusDescription ?? b.BookingStatusName;
                }
            }
            return "";
        }
        /// <summary>
        /// Useful booking request information as text-only, for use in Calendar Events description or other text-only places.
        /// </summary>
        /// <param name="BookingRequestID"></param>
        /// <returns></returns>
        public static string GetBookingRequestInformationForProviderAsTextOnly(int BookingRequestID)
        {
            var sb = new System.Text.StringBuilder();
            dynamic summary = LcData.Booking.GetBookingRequestForUser(BookingRequestID, 0, true);
            var pricingSummary = LcData.Booking.GetPricingSummary(summary);
            var pricingSummaryGroups = LcData.Booking.GetPricingSummaryGroups(summary.PricingEstimateID, summary);
            
            sb.AppendLine("Pricing summary:");
            sb.Append(ASP.LcPricingView.TextOnlyProviderPricingSummary(pricingSummary, pricingSummaryGroups));

            sb.AppendLine("Special instructions:");
            sb.AppendLine(summary.SpecialRequests);

            sb.AppendLine("Payment:");
            sb.AppendLine(GetBookingPaymentInformation(summary, LcData.UserInfo.UserType.Provider));
            
            return sb.ToString();
        }
        /// <summary>
        /// Get the text with payment information for a booking to be showed to the requested user-type
        /// </summary>
        /// <param name="booking"></param>
        /// <param name="userType"></param>
        /// <returns></returns>
        public static string GetBookingPaymentInformation(dynamic booking, LcData.UserInfo.UserType userType, dynamic itsUserData = null)
        {
            string msg = "";

            switch (userType)
            {
                case UserInfo.UserType.Provider:

                    // There is no message on Cancelled, what means nothing must be showed:
                    const string msgProviderCanceled = "";
                    // Message for request or confirmed but not payed.
                    const string msgProviderNotPayed = "We have received payment from the client. You'll receive payment after the appointment is successfully completed.";

                    switch ((int)booking.BookingRequestStatusID)
                    {
                        // Request complete, waiting for provider response
                        case 2:
                            msg = msgProviderNotPayed;
                            break;
                        // Request accepted, is a confirmed booking
                        case 7:
                            switch ((int)booking.BookingStatusID)
                            {
                                // Its a confirmed booking, but still not payed (waiting for work, performed or in dispute)
                                case 1:
                                case 2:
                                case 3:
                                case 5:
                                    msg = msgProviderNotPayed;
                                    break;
                                // Completed and payed
                                case 4:
                                    msg = "{0:c} transfer to checking account ending in scheduled for {1:d}";
                                    break;
                                // Cancelled by customer
                                case 6:
                                    msg = msgProviderCanceled;
                                    break;
                            }
                            break;
                        // Any other case: incomplete request, expired, denied or cancelled:
                        default:
                            msg = msgProviderCanceled;
                            break;
                    }

                    return String.Format(msg,
                        booking.TotalPrice,
                        booking.PaymentDate ?? "<date not available>"
                    );
                
                case UserInfo.UserType.Customer:

                    const string msgCustomer = "<em><strong>Your credit card has not been charged</strong></em> and the authorization should expire shortly.";

                    switch ((int)booking.BookingRequestStatusID)
                    {
                        // Request complete, waiting for provider response
                        case 2:
                            msg = @"Your card ending in {2} will be authorized for {0:c}. 
                            If your request is accepted (within 18 hours), we’ll charge your card the day {1}
                            performs the scheduled services.
                            We'll withhold payment to {1} until after the service is completed.";
                            break;
                        // Request accepted, is a confirmed booking
                        case 7:
                            switch ((int)booking.BookingStatusID)
                            {
                                // Its a confirmed booking, but still not payed (waiting for work, performed or in dispute)
                                case 1:
                                case 2:
                                case 3:
                                case 5:
                                    msg = "Your card ending in {2} has been authorized for {0:c}. We’ll charge your card the day {1} performs the scheduled services.";
                                    break;
                                // Completed and payed
                                case 4:
                                    msg = "Your payment of {0:c} has been successfully received, and all payments have been released to {1}";
                                    break;
                                // Cancelled by customer
                                case 6:
                                    msg = GetBookingPaymentCustomerCancelledTemplate(booking);
                                    break;
                            }
                            break;
                        // Request cancelled
                        case 4:
                            msg = GetBookingPaymentCustomerCancelledTemplate(booking);
                            break;
                        // Any other case: incomplete request, expired, denied:
                        default:
                            msg = msgCustomer;
                            break;
                    }
                    return String.Format(
                        msg,
                        booking.TotalPrice,
                        (itsUserData == null ? "your provider" : itsUserData.FirstName),
                        LcEncryptor.Decrypt(booking.PaymentCustomerCardLastDigits)
                    );
            }

            // Generic message on any other case (if all is complete, never must be showed):
            return String.Format(
                "Total to be paid on {0:d}",
                booking.PaymentDate ?? "<date not available>"
            );
        }

        /// <summary>
        /// Utility for use in GetBookingPaymentInformation when a booking/request is cancelled
        /// by customer: message template for the customer
        /// </summary>
        /// <param name="booking"></param>
        /// <param name="userType"></param>
        /// <param name="itsUserData"></param>
        /// <returns></returns>
        public static string GetBookingPaymentCustomerCancelledTemplate(dynamic booking)
        {
            var t = "Your credit card has ben charged {0:c} ";

            var fullAmount = booking.TotalPrice == booking.TotalRefunded;
            var halfPercent = booking.SubtotalRefunded > 0 && booking.SubtotalPrice / 2 == booking.SubtotalRefunded;
            var onlyFees = booking.FeeRefunded > 0 && booking.SubtotalRefunded == 0;
            var inTime = true;

            // Checking ConfirmedDateStart against the DateRefunded and the 'HoursRequired before booking'
            // from the cancellation policy to know if cancellation was done in time or not.
            // IMPORTANT: Dates from a BookingRequest are not checked since there is no support for
            // customer cancellations previous booking confirmation.
            if (N.D(booking.DateRefunded) != null &&
                N.D(booking.ConfirmedDateStart) != null)
            {
                // Compare booking date with date refunded
                using (var db = Database.Open("sqlloco"))
                {
                    var hours = db.QueryValue(@"
                        SELECT  C.HoursRequired
                        FROM    CancellationPolicy As C
                        WHERE   C.CAncellationPolicyID = @0
                    ", booking.CancellationPolicyID);

                    inTime = booking.DateRefunded < booking.ConfirmedDateStart.AddHours(0 - hours);
                }
            }
            
            t += fullAmount ? "(full amount) " : "";
            t += halfPercent ? "(50% plus booking fees) " : "";
            t += onlyFees ? "(booking fees) " : "";

            t += "per the cancellation policy below";

            if (!inTime)
            {
                t += " as you did not cancel within the allotted time to receive any refund";
            }

            t += ".";

            return t;
        }

        /// <summary>
        /// Get a string in text-only format to be used as the CalendarEvent Description field with the
        /// details of the booking request (hidden still the contact data)
        /// This description contains data oriented for PROVIDERS ONLY
        /// </summary>
        /// <param name="BookingRequestID"></param>
        /// <returns></returns>
        public static string GetBookingRequestEventDescription(int BookingRequestID)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(GetBookingRequestInformationForProviderAsTextOnly(BookingRequestID));
            // Previous ends in new-line but we add another new-line to get an empty line, and the phone:
            sb.AppendLine("\nCall Loconomics for help: (415) 735-6025");
            sb.AppendLine("Full details at " + LcUrl.LangUrl + LcData.Booking.GetUrlPathForBookingRequest(BookingRequestID));
            return sb.ToString();
        }
        /// <summary>
        /// Get a string in text-only format to be used as the CalendarEvent Description field with the
        /// details of the confirmed booking (this shows contact data too).
        /// This description contains data oriented for PROVIDERS ONLY
        /// </summary>
        /// <param name="BookingID"></param>
        /// <returns></returns>
        public static string GetBookingEventDescription(int BookingID)
        {
            var booking = GetBookingBasicInfo(BookingID);
            // We need customer full name and phones:
            var customer = LcData.UserInfo.GetUserRowWithContactData(booking.CustomerUserID);
            var phones = customer.MobilePhone ?? customer.AlternatePhone;
            if (!string.IsNullOrEmpty(customer.MobilePhone) &&
                !string.IsNullOrEmpty(customer.AlternatePhone))
            {
                phones = customer.MobilePhone + ", " + customer.AlternatePhone;
            }
            var sb = new System.Text.StringBuilder();
            sb.AppendFormat("{0} {1}'s phone number: {2}\n", customer.FirstName, customer.LastName, phones);
            // Rest is the same as booking request:
            sb.Append(GetBookingRequestEventDescription(booking.BookingRequestID));
            return sb.ToString();
        }
        /// <summary>
        /// Get the location/address full information in one line of plain-text, to be used
        /// for example as a CalendarEvent.Location
        /// </summary>
        /// <param name="BookingID"></param>
        /// <returns></returns>
        public static string GetBookingLocationAsOneLineText(int BookingID)
        {
            var booking = GetBookingForUser(BookingID, 0, true);
            if (String.IsNullOrEmpty(booking.StateProvinceCode))
                // There is no address:
                return "";
            // Else, build the address one-line:
            return String.Format("{0} {1} - {2} ({4}) {5}{6}",
                booking.AddressLine1,
                booking.AddressLine2,
                booking.City,
                booking.StateProvinceName,
                booking.StateProvinceCode,
                booking.PostalCode,
                !String.IsNullOrEmpty(booking.LocationSpecialInstructions) ? string.Format(" ({0})", booking.LocationSpecialInstructions) : ""
            );
        }
        #endregion

        #region Booking Request updates
        public static void SetBookingRequestTransaction(int bookingRequestID, string transactionID)
        {
            using (var db = Database.Open("sqlloco"))
            {
                /*
                    * Save Braintree Transaction ID in the Booking Request and card digits
                    * and updating State to 'Booking Request Completed' (2:completed)
                    */
                db.Execute(@"
                    UPDATE  BookingRequest
                    SET     PaymentTransactionID = @1
                    WHERE   BookingRequestID = @0
                ", bookingRequestID, transactionID);
            }
        }
        #endregion

        #region Create Booking
        #region SQLs
        public const string sqlGetBookingRequestDates = @"
            SELECT  TOP 1
                    R.PreferredDateID,
                    R.AlternativeDate1ID,
                    R.AlternativeDate2ID
            FROM    BookingRequest As R
            WHERE   R.BookingRequestID = @0
                    -- Only bookings 'created', but not incomplete, confirmed, cancelled or any other one
                     AND
                    R.BookingRequestStatusID = 2
        ";
        public const string sqlConfirmBooking = @"
            -- Parameters
            DECLARE @BookingRequestID int, @ConfirmedDateID int
            SET @BookingRequestID = @0
            SET @ConfirmedDateID = @1

            DECLARE @BookingID int
            SET @BookingID = -1

            BEGIN TRY
                BEGIN TRAN

                /*
                 * Creating the Confirmed Booking Record
                 */
                INSERT INTO Booking (
                    BookingRequestID, ConfirmedDateID,
                    BookingStatusID,
                    TotalPricePaidByCustomer, TotalServiceFeesPaidByCustomer, 
                    TotalPaidToProvider, TotalServiceFeesPaidByProvider, 
                    PricingAdjustmentApplied,
                    CreatedDate, UpdatedDate, ModifiedBy
                ) VALUES (
                    @BookingRequestID, @ConfirmedDateID,
                    1, --confirmed
                    -- Initial price data to null
                    -- (booking request prices are estimated, here only paid prices go)
                    null, null, null, null,
                    0, -- not applied price adjustement (now, almost)
                    getdate(), getdate(), 'sys'
                )
                SET @BookingID = @@Identity

                -- Removing non needed CalendarEvents:
                DELETE FROM CalendarEvents
                WHERE ID IN (
                    SELECT TOP 1 PreferredDateID FROM BookingRequest
                    WHERE BookingRequestID = @BookingRequestID AND PreferredDateID <> @ConfirmedDateID
                    UNION
                    SELECT TOP 1 AlternativeDate1ID FROM BookingRequest
                    WHERE BookingRequestID = @BookingRequestID AND AlternativeDate1ID <> @ConfirmedDateID
                    UNION
                    SELECT TOP 1 AlternativeDate2ID FROM BookingRequest
                    WHERE BookingRequestID = @BookingRequestID AND AlternativeDate2ID <> @ConfirmedDateID
                )

                /*
                 * Update Availability of the CalendarEvent record for the ConfirmedDateID,
                 * from 'tentative' to 'busy'
                 */
                UPDATE  CalendarEvents
                SET     CalendarAvailabilityTypeID = 2
                WHERE   Id = @ConfirmedDateID

                /*
                 * Updating Booking Request status, and removing references to the 
                 * user selected dates (there is already a confirmed date in booking).
                 */
                UPDATE  BookingRequest
                SET     BookingRequestStatusID = 7, -- accepted
                        PreferredDateID = null,
                        AlternativeDate1ID = null,
                        AlternativeDate2ID = null
                WHERE   BookingRequestID = @BookingRequestID

                COMMIT TRAN

                -- We return sucessful operation with Error=0
                -- and attach the BookingID created instead of an ErrorMessage
                SELECT 0 As Error, @BookingID As ID
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0
                    ROLLBACK TRAN
                -- We return error number and message
                SELECT ERROR_NUMBER() As Error, ERROR_MESSAGE() As ErrorMessage
            END CATCH
        ";
        #endregion

        /// <summary>
        /// Create a booking by confirming an existant Booking Request,
        /// this last gets updated to reflect the new status.
        /// Payment is managed, but messaging is not (caller code can sent it if result.Error == 0)
        /// </summary>
        /// <param name="bookingRequestID"></param>
        /// <param name="dateType">A string value from: PREFERRED, ALTERNATIVE1, ALTERNATIVE2;
        /// that date/event from the booking request will be confirmed.</param>
        /// <param name="db"></param>
        /// <returns>An object with almost an integer/numeric property 'Error', 0 when there is no
        /// error; if there is error (!= 0), ErrorMessage contains a string, otherwise other
        /// properties are added with information: BookingID for the created booking.
        /// Error > 0: Database error during confirmation.
        /// Error < 0: verifications errors:
        /// -1: there is no date to confirm, maybe booking is already confirmed or there is not exists.
        /// -2: provider not available that date
        /// -3: payment error
        /// </returns>
        public static SqlGenericResult ConfirmBooking(int bookingRequestID, string dateType, Database db = null)
        {
            var owned = db == null;
            if (owned)
                db = Database.Open("sqlloco");

            try
            {
                // Getting the date
                var dateID = 0;
                var dates = db.QuerySingle(sqlGetBookingRequestDates, bookingRequestID);
                if (dates != null)
                {
                    switch (dateType.ToUpper())
                    {
                        case "PREFERRED":
                            if (dates.PreferredDateID != null)
                            {
                                dateID = dates.PreferredDateID;
                            }
                            break;
                        case "ALTERNATIVE1":
                            if (dates.AlternativeDate1ID != null)
                            {
                                dateID = dates.AlternativeDate1ID;
                            }
                            break;
                        case "ALTERNATIVE2":
                            if (dates.AlternativeDate2ID != null)
                            {
                                dateID = dates.AlternativeDate2ID;
                            }
                            break;
                    }
                }
                if (dateID == 0)
                {
                    return new SqlGenericResult
                    {
                        Error = -1,
                        ErrorMessage = "We're having issues finding your booking request. Please contact us at support@loconomics.com."
                    };
                }

                // Change the event to be 'transparent'(4) for a while to don't
                // affect the availability check.
                // And get the required information from the event to do the
                // availability check
                if (!LcCalendar.DoubleCheckEventAvailability(dateID))
                {
                    return new SqlGenericResult
                    {
                        Error = -2,
                        // Text: message can end being showed to both customer and provider (but can be customized
                        // on each page executing this), and must care that the time was available when selected
                        // but is not now because a (very) recent change (race condition).
                        ErrorMessage = "The choosen time is not available, it conflicts with a recent appointment!"
                    };
                }

                // Creating booking and updating BookingRequest
                var result = new SqlGenericResult(db.QuerySingle(sqlConfirmBooking, bookingRequestID, dateID));

                // On no errors:
                if (result.Error == 0)
                {
                    var dateInfo = LcCalendar.GetBasicEventInfo(dateID, db);

                    // SINCE #508, Customer is not charged on confirming the booking, else the date of the service;
                    // NOW here we authorize a transaction for lower than 7 days for service date from now, or
                    // nothing on other cases since we cannot ensure the authorization would persist more than
                    // that time, and transaction gets postponed for the service date on 'settle transaction'.
                    if ((dateInfo.StartTime - DateTime.Now) < TimeSpan.FromDays(7)) 
                    {
                        // Get card from transaction
                        string transactionID = db.QueryValue("SELECT coalesce(PaymentTransactionId, '') FROM BookingRequest WHERE BookingRequestID = @0", bookingRequestID);

                        var authorizationError = AuthorizeBookingTransaction(transactionID, result.ID, db);

                        if (!String.IsNullOrEmpty(authorizationError))
                        {
                            return new SqlGenericResult
                            {
                                Error = -3,
                                ErrorMessage = authorizationError
                            };
                        }
                    }

                    // Next commented code is from previous #508: 'charge/settle' is done in a ScheduledTask now
                    /*
                    // Get booking request TransactionID
                    string tranID = N.DE(db.QueryValue(sqlGetTransactionIDFromBookingRequest, bookingRequestID));
                    // Charge customer:
                    var settleResult = SettleBookingTransaction(tranID, result.BookingID, db);
                    if (settleResult != null)
                    {
                        return new SqlGenericResult
                        {
                            Error = -3,
                            ErrorMessage = settleResult
                        };
                    }
                    */

                    // Update the CalendarEvent to include contact data,
                    // but this is not so important as the rest because of that it goes
                    // inside a try-catch, it doesn't matter if fails, is just a commodity
                    // (customer and provider can access contact data from the booking).
                    try
                    {
                        var description = LcData.Booking.GetBookingEventDescription(result.ID);
                        var location = LcData.Booking.GetBookingLocationAsOneLineText(result.ID);
                        db.Execute(@"
                                UPDATE  CalendarEvents
                                SET     Description = @1,
                                        Location = @2
                                WHERE   Id = @0
                            ", dateID, description, location);
                    }
                    catch { }
                }

                return result;
            }
            catch (Exception ex)
            {
                // Replay exception, we just need the finally to close the connection properly
                throw ex;
            }
            finally
            {
                if (owned)
                    db.Dispose();
            }
        }

        /// <summary>
        /// If the given transactionID is a Card Token, it performs a transaction to authorize, without charge/settle,
        /// the amount on the card.
        /// This process ensures the money is available for the later moment the charge happens (withing the authorization
        /// period, the worse case 7 days) using 'SettleBookingTransaction', or manage preventively any error that arises.
        /// The transactionID generated (if any) is updated on database properly.
        /// </summary>
        /// <param name="paymentTransactionID"></param>
        /// <param name="bookingID"></param>
        /// <param name="db"></param>
        /// <returns>An error message or null on success</returns>
        [Obsolete("Use LcRest.Booking methods, updated for new DB scheme, constraints and flags")]
        public static string AuthorizeBookingTransaction(string paymentTransactionID, int bookingID, Database db = null)
        {
            var owned = db == null;
            if (owned)
                db = Database.Open("sqlloco");

            try
            {
                if (!LcPayment.IsFakeTransaction(paymentTransactionID))
                {
                    if (paymentTransactionID.StartsWith(LcPayment.TransactionIdIsCardPrefix))
                    {
                        var cardToken = paymentTransactionID.Substring(LcPayment.TransactionIdIsCardPrefix.Length);

                        if (!String.IsNullOrWhiteSpace(cardToken))
                        {
                            // Transaction authorization, so NOT charge/settle now
                            var authorizationError = LcPayment.SaleBookingTransaction(bookingID, cardToken, false);

                            if (!String.IsNullOrEmpty(authorizationError))
                            {
                                return authorizationError;
                            }
                        }
                        else
                        {
                            return "Transaction or card identifier gets lost, payment cannot be performed.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Replay exception, we just need the finally to close the connection properly
                throw ex;
            }
            finally
            {
                if (owned)
                    db.Dispose();
            }

            return null;
        }

        /// <summary>
        /// Charge customer for the booking request transaction
        /// </summary>
        /// <param name="paymentTransactionID"></param>
        /// <param name="bookingID"></param>
        /// <param name="db"></param>
        /// <returns>A string with an error message or null on success.
        /// </returns>
        [Obsolete("Use LcRest.Booking methods, that enforce new constraints, save data and use updated LcPayment methods")]
        public static string SettleBookingTransaction(string paymentTransactionID, int bookingID, Database db = null)
        {
            var owned = db == null;
            if (owned)
                db = Database.Open("sqlloco");

            try
            {
                // Charge total amount of booking request to the customer (Submit to settlement the transaction)
                if (!LcPayment.IsFakeTransaction(paymentTransactionID))
                {
                    // Since #508, we can have an authorized transaction to be settle or a saved card to perform
                    // the charge without previous authorization;
                    // On card cases, we saved the card in the transactionID, its just a 'CARD:' prefix
                    // followed by the card token
                    if (paymentTransactionID.StartsWith(LcPayment.TransactionIdIsCardPrefix))
                    {
                        var cardToken = paymentTransactionID.Substring(LcPayment.TransactionIdIsCardPrefix.Length);
                        
                        // Do transaction, charging/settling now
                        string saleError = LcPayment.SaleBookingTransaction(bookingID, cardToken, true);
                        if (saleError != null)
                            return saleError;
                    }
                    else
                    {
                        // We have an authorized transaction to be settle, AKA charge card
                        string paymentError = LcPayment.SettleTransaction(paymentTransactionID);
                        if (paymentError != null)
                        {
                            return paymentError;
                        }
                    }
                }

                // Update booking database information, setting amount payed by customer
                db.Execute(@"
                        DECLARE @price decimal(7, 2)
                        DECLARE @fees decimal(7, 2)
                        DECLARE @BookingRequestID int
    
                        SELECT  @BookingRequestID = BookingRequestID
                        FROM    Booking
                        WHERE   BookingID = @0

                        SELECT
                                @price = coalesce(TotalPrice, 0),
                                @fees = coalesce(FeePrice, 0)
                        FROM
                                pricingestimate
                        WHERE
                                PricingEstimateID = (SELECT PricingEstimateID FROM BookingRequest WHERE BookingRequestID = @BookingRequestID)

                        UPDATE  Booking
                        SET     TotalPricePaidByCustomer = @price,
                                TotalServiceFeesPaidByCustomer = @fees
                        WHERE   BookingId = @0
                    ", bookingID);
            }
            catch (Exception ex)
            {
                // Replay exception, we just need the finally to close the connection properly
                throw ex;
            }
            finally
            {
                if (owned)
                    db.Dispose();
            }

            return null;
        }
        #endregion

        #region Actions on bookings, modifications
        /// <summary>
        /// Invalide a Booking Request setting it as 'cancelled', 'declined',
        /// or 'expired', preserving the main data but removing some unneded
        /// data as the events-dates and the address (if this can be removed)
        /// and every reference to that events and address from the BookingRequest
        /// table.
        /// The desired new BookingStatusID must be provided, but is filtered and
        /// if is not a valid ID an exception is throw.
        /// If there is a TransactionID related to the BookingRequest, that transaction
        /// is refunded if needed.
        /// List of BookingRequestStatusID:
        /// BookingRequestStatusID	BookingRequestStatusName
        /// 3	cancelled
        /// 4	denied
        /// 5	expired
        /// </summary>
        /// <param name="BookingRequestID"></param>
        public static SqlGenericResult InvalidateBookingRequest(int BookingID, int BookingStatusID)
        {
            if (!(new int[] { 3, 4, 5 }).Contains<int>(BookingStatusID))
            {
                throw new Exception(String.Format(
                    "BookingStatusID '{0}' is not valid to invalidate the booking request",
                    BookingStatusID));
            }

            var sqlGetTransactionAndUsers = @"
                SELECT  PaymentTransactionID,
                        ClientUserID,
                        ServiceProfessionalUserID
                FROM    Booking
                WHERE   BookingID = @0
            ";

            var sqlInvalidateBookingRequest = @"EXEC InvalidateBookingRequest @0, @1";

            using (var db = Database.Open("sqlloco"))
            {
                // Check cancellation policy and get quantities to refund
                var refund = GetCancellationAmountsForBookingRequest(BookingID, BookingStatusID, db);

                // Get booking info
                var booking = db.QuerySingle(sqlGetTransactionAndUsers, BookingID);

                string result = ManageRefundTransaction(booking.PaymentTransactionID, refund, booking.CustomerUserID, booking.ProviderUserID);

                if (result != null)
                    return (dynamic)new SqlGenericResult { Error = 9999, ErrorMessage = result };

                // All goes fine with payment proccessing, continue

                // Save refund quantities in booking PricingEstimateID
                SaveRefundAmounts(refund, db);

                // Invalidate in database the booking request:    
                return db.QuerySingle(sqlInvalidateBookingRequest,
                    BookingID,
                    BookingStatusID);
            }
        }

        /// <summary>
        /// Given a database saved transactionID (can be a card identifier or nothing)
        /// and a 'cancellation refund' record with amounts of money to refund and remaining,
        /// it performs for either a booking or booking-request, the refund of
        /// money for a cancellation.
        /// </summary>
        /// <param name="tranID"></param>
        /// <param name="refund"></param>
        /// <returns></returns>
        public static string ManageRefundTransaction(string tranID, dynamic refund, int customerID, int providerID)
        {
            string result = null;

            try
            {
                if (!LcPayment.IsFakeTransaction(tranID))
                {
                    if (tranID.StartsWith(LcPayment.TransactionIdIsCardPrefix))
                    {
                        // For saved cards, there is no transaction, we need to do
                        // one to refund money, or just delete the card if was a full-refund,
                        // all cases managed at LcPayment:
                        var creditCard = tranID.Substring(LcPayment.TransactionIdIsCardPrefix.Length);
                        result = LcPayment.DoTransactionToRefundFromCard(creditCard, refund, customerID, providerID);
                    }
                    else
                    {
                        // Different calls for total and partial refunds
                        if (refund.IsTotalRefund)
                        {
                            result = LcPayment.RefundTransaction(tranID);
                        }
                        else
                        {
                            // Partial refund could be given with zero amount to refund (because cancellation
                            // policy sets no refund), execute payment partial refund only with no zero values
                            if (refund.TotalRefunded != 0)
                            {
                                // Refund does the payment release
                                result = LcPayment.RefundTransaction(tranID, refund.TotalRefunded);
                            }
                            else
                            {
                                // Marketplace #408: just after refund to the customer its amount, pay the rest amount
                                // to the provider (and fees to us)
                                if (result == null)
                                    result = LcPayment.ReleaseTransactionFromEscrow(tranID);
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            return result;
        }

        public static SqlGenericResult InvalidateBooking(int BookingID, int BookingStatusID)
        {
            if (!(new int[] { 6, 7 }).Contains<int>(BookingStatusID))
            {
                throw new Exception(String.Format(
                    "BookingStatusID '{0}' is not valid to invalidate the booking",
                    BookingStatusID));
            }

            var sqlGetTransactionAndUsers = @"
                SELECT  BookingRequest.PaymentTransactionID,
                        BookingRequest.CustomerUserID,
                        BookingRequest.ProviderUserID
                FROM    BookingRequest
                         INNER JOIN
                        Booking
                          ON BookingRequest.BookingRequestID = Booking.BookingRequestID
                WHERE   BookingID = @0
            ";
            var sqlInvalidateBooking = @"
                -- Parameters
                DECLARE @BookingID int, @BookingStatusID int
                SET @BookingID = @0
                SET @BookingStatusID = @1

                DECLARE @AddressID int
                DECLARE @BookingRequestID int

                SELECT @BookingRequestID = BookingRequestID
                FROM    Booking
                WHERE   BookingID = @BookingID

                BEGIN TRY
                    BEGIN TRAN

                    -- Get Service Address ID to be (maybe) removed later
                    SELECT  @AddressID = AddressID
                    FROM    BookingRequest
                             INNER JOIN
                            Booking
                              ON BookingRequest.BookingRequestID = Booking.BookingRequestID
                    WHERE   BookingID = @BookingID

                    -- Removing CalendarEvents:
                    DELETE FROM CalendarEvents
                    WHERE ID IN (
                        SELECT TOP 1 ConfirmedDateID FROM Booking
                        WHERE BookingID = @BookingID
                    )

                    /*
                     * Updating Booking status, and removing references to the 
                     * user selected dates.
                     */
                    UPDATE  Booking
                    SET     BookingStatusID = @BookingStatusID,
                            ConfirmedDateID = null
                    WHERE   BookingID = @BookingID
                    /*
                     * Updating Booking Request, removing reference to the address
                     */
                    UPDATE  BookingRequest
                    SET     AddressID = null
                    WHERE   BookingRequestID = @BookingRequestID

                    -- Removing Service Address, if is not an user saved location (it has not AddressName)
                    DELETE FROM ServiceAddress
                    WHERE AddressID = @AddressID
                          AND (SELECT count(*) FROM Address As A WHERE A.AddressID = @AddressID AND AddressName is null) = 1
                    DELETE FROM Address
                    WHERE AddressID = @AddressID
                           AND
                          AddressName is null

                    COMMIT TRAN

                    -- We return sucessful operation with Error=0
                    SELECT 0 As Error, null As ErrorMessage
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0
                        ROLLBACK TRAN
                    -- We return error number and message
                    SELECT ERROR_NUMBER() As Error, ERROR_MESSAGE() As ErrorMessage
                END CATCH
            ";
            using (var db = Database.Open("sqlloco"))
            {
                // Check cancellation policy and get quantities to refund
                var refund = GetCancellationAmountsForBooking(BookingID, BookingStatusID, db);

                // Get booking info
                var booking = db.QuerySingle(sqlGetTransactionAndUsers, BookingID);

                string result = ManageRefundTransaction(booking.PaymentTransactionID, refund, booking.CustomerUserID, booking.ProviderUserID);

                if (result != null)
                    return new SqlGenericResult { Error = 9999, ErrorMessage = result };

                // All goes fine with payment proccessing, continue

                // Save refund quantities in booking PricingEstimateID
                SaveRefundAmounts(refund, db);

                // Invalidate in database the booking:
                return new SqlGenericResult(
                    db.QuerySingle(sqlInvalidateBooking,
                        BookingID,
                        BookingStatusID
                    )
                );
            }
        }
        public static void UpdateBookingTransactionID(string oldTranID, string newTranID)
        {
            using (var db = Database.Open("sqlloco"))
            {
                db.Execute(@"
                    UPDATE  BookingRequest
                    SET     TransactionID = @1
                    WHERE   TransactionID = @0
                ", oldTranID, newTranID);
            }
        }
        #endregion

        #region Cancellation policy
        /// <summary>
        /// Currently, default cancellation policy is Flexible:3
        /// </summary>
        public const int DefaultCancellationPolicyID = 3;
        private const string sqlViewPricingAndPolicy = @"
            SELECT  R.PricingEstimateID
                    ,R.BookingRequestStatusID
                    ,R.CancellationPolicyID
                    ,C.HoursRequired
                    ,C.RefundIfCancelledBefore
                    ,C.RefundIfCancelledAfter
                    ,C.RefundOfLoconomicsFee
                    ,P.SubtotalPrice
                    ,P.TotalPrice
                    ,P.FeePrice
                    ,P.PFeePrice
            FROM    BookingRequest As R
                        INNER JOIN CancellationPolicy As C
                        ON R.CancellationPolicyID = C.CAncellationPolicyID
                        INNER JOIN PricingEstimate As P
                        ON R.PricingEstimateID = P.PricingEstimateID
        ";
        public static dynamic GetCancellationAmountsForBookingRequest(int bookingRequestID, int changingToBookingRequestStatusID, Database db)
        {
            var fullRefund = false;
            var confirmedDate = System.Data.SqlTypes.SqlDateTime.MinValue;

            // If new desired BookingRequestStatusID is not 'cancelled by customer' (different of 4)
            // a total refund is done, no cancellation policy applies (is
            // cancelled by provider or system)
            if (changingToBookingRequestStatusID != 4)
            {
                fullRefund = true;
            }
            else
            {
                // Get confirmated booking date (start date and time):
                confirmedDate = db.QueryValue(@"
                    SELECT  E.StartTime
                    FROM    Booking As B
                                INNER JOIN CalendarEvents As E
                                ON B.ConfirmedDateID = E.Id
                    WHERE   B.BookingRequestID = @0
                ", bookingRequestID);
                // TODO: Just now (issue #142, 20121122), a booking request cannot be
                // cancelled by customer, this code will not execute, BUT if
                // someday need be work, confirmedDate must be retrieve some valid
                // date, because is not still confirmed, confirmedDate will be NULL
                // get the nearest proposed date (in bookingrequest table)
                // Next code is to avoid calls to null value:
                if (confirmedDate.IsNull)
                    confirmedDate = System.Data.SqlTypes.SqlDateTime.MinValue;
            }

            var b = db.QuerySingle(sqlViewPricingAndPolicy + @"
                WHERE   BookingRequestID = @0
            ", bookingRequestID);

            return GetCancellationAmountsFor(b, confirmedDate.Value, fullRefund, db);
        }
        public static dynamic GetCancellationAmountsForBooking(int bookingID, int changingToBookingStatusID, Database db)
        {
            var fullRefund = false;
            var confirmedDate = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

            // If new desired BookingStatusID is not 'cancelled by customer' (different of 6)
            // a total refund is done, no cancellation policy applies (is
            // cancelled by provider or system)
            if (changingToBookingStatusID != 6)
            {
                fullRefund = true;
            }
            else
            {
                // Get confirmated booking date (start date and time):
                confirmedDate = (DateTime)db.QueryValue(@"
                    SELECT  E.StartTime
                    FROM    Booking As B
                                INNER JOIN CalendarEvents As E
                                ON B.ConfirmedDateID = E.Id
                    WHERE   B.BookingID = @0
                ", bookingID);
            }

            var b = db.QuerySingle(sqlViewPricingAndPolicy + @"
                     INNER JOIN Booking As B
                      ON B.BookingRequestID = R.BookingRequestID
                WHERE   BookingID = @0
            ", bookingID);

            return GetCancellationAmountsFor(b, confirmedDate, fullRefund, db);
        }
        public static dynamic GetCancellationAmountsFor(dynamic pricingAndPolicy, DateTime confirmedDate, bool fullRefund, Database db)
        {
            dynamic result = null;

            if (fullRefund)
            {
                // TOTAL REFUND
                result = new
                {
                    PricingEstimateID = pricingAndPolicy.PricingEstimateID,
                    IsTotalRefund = true,
                    SubtotalRefunded = pricingAndPolicy.SubtotalPrice,
                    FeeRefunded = pricingAndPolicy.FeePrice,
                    TotalRefunded = pricingAndPolicy.TotalPrice,
                    DateRefunded = DateTime.Now,
                    SubtotalRemaining = 0M,
                    FeeRemaining = 0M,
                    TotalRemaining = 0M
                };
            }
            else
            {
                // PARTIAL REFUND OR NO REFUND, based Cancellation Policy
                if (DateTime.Now < confirmedDate.AddHours(0 - pricingAndPolicy.HoursRequired))
                {
                    // BEFORE limit date
                    decimal subr = (decimal)pricingAndPolicy.SubtotalPrice * (decimal)pricingAndPolicy.RefundIfCancelledBefore,
                        feer = (decimal)pricingAndPolicy.FeePrice * (decimal)pricingAndPolicy.RefundOfLoconomicsFee;
                    result = new
                    {
                        PricingEstimateID = pricingAndPolicy.PricingEstimateID,
                        IsTotalRefund = false,
                        SubtotalRefunded = subr,
                        FeeRefunded = feer,
                        TotalRefunded = subr + feer,
                        DateRefunded = DateTime.Now,
                        SubtotalRemaining = (decimal)pricingAndPolicy.SubtotalPrice - subr,
                        FeeRemaining = (decimal)pricingAndPolicy.FeePrice - feer,
                        TotalRemaining = (decimal)pricingAndPolicy.TotalPrice - subr - feer
                    };
                }
                else
                {
                    // AFTER limit date
                    decimal subr = (decimal)pricingAndPolicy.SubtotalPrice * (decimal)pricingAndPolicy.RefundIfCancelledAfter;
                    result = new
                    {
                        PricingEstimateID = pricingAndPolicy.PricingEstimateID,
                        IsTotalRefund = false,
                        SubtotalRefunded = subr,
                        // Fees never are refunded after limit date
                        FeeRefunded = 0,
                        TotalRefunded = subr,
                        DateRefunded = DateTime.Now,
                        SubtotalRemaining = (decimal)pricingAndPolicy.SubtotalPrice - subr,
                        FeeRemaining = (decimal)pricingAndPolicy.FeePrice,
                        TotalRemaining = (decimal)pricingAndPolicy.TotalPrice - subr
                    };
                }
            }

            return result;
        }
        /// <summary>
        /// Update PricingEstimate with the data give at refunData, that contains
        /// exactly the struct returned by GetCancellationAmountsFor method, with
        /// PricingEstimateID, date and amounts refunded
        /// </summary>
        /// <param name="refundData"></param>
        /// <param name="db"></param>
        public static void SaveRefundAmounts(dynamic refundData, Database db = null)
        {
            var ownDb = false;
            if (db == null)
            {
                ownDb = true;
                db = Database.Open("sqlloco");
            }

            db.Execute(@"
                UPDATE  PricingEstimate
                SET     SubtotalRefunded = @1
                        ,FeeRefunded = @2
                        ,TotalRefunded = @3
                        ,DateRefunded = @4
                WHERE   PricingEstimateID = @0
                ",
                refundData.PricingEstimateID,
                refundData.SubtotalRefunded,
                refundData.FeeRefunded,
                refundData.TotalRefunded,
                refundData.DateRefunded
            );

            if (ownDb)
                db.Dispose();
        }

        /// <summary>
        /// Get a dynamic record with fields: CancellationPolicyID and CancellationPolicyName
        /// from the provider preference, or null if it has none.
        /// </summary>
        /// <param name="providerUserID"></param>
        /// <param name="positionID"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static dynamic GetProviderCancellationPolicy(int providerUserID, int positionID, Database db)
        {
            return db.QuerySingle(@"
                SELECT  U.CancellationPolicyID
                        ,C.CancellationPolicyName
                FROM    UserProfilePositions As U
                         INNER JOIN
                        CancellationPolicy As C
                          ON U.CancellationPolicyID = C.CancellationPolicyID
                             AND U.LanguageID = C.LanguageID
                             AND U.CountryID = C.CountryID
                WHERE   U.UserID = @0
                         AND U.PositionID = @1
                         AND U.LanguageID = @2
                         AND U.CountryID = @3
            ", providerUserID, positionID, LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID());
        }
        /// <summary>
        /// Gets the preferred cancellationPolicyID of the provider for the position
        /// or the DefaultCancellationPolicyID if provider has not the preference.
        /// </summary>
        /// <param name="providerUserID"></param>
        /// <param name="positionID"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static int GetProviderCancellationPolicyID(int providerUserID, int positionID, Database db = null)
        {
            var disposeDb = db == null;
            if (db == null)
                db = Database.Open("sqlloco");

            int rtn = N.D(db.QueryValue(@"
                SELECT  U.CancellationPolicyID
                FROM    UserProfilePositions As U
                WHERE   U.UserID = @0
                         AND U.PositionID = @1
                         AND U.LanguageID = @2
                         AND U.CountryID = @3
            ", providerUserID, positionID, LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID()))
            ?? DefaultCancellationPolicyID;

            if (disposeDb)
                db.Dispose();
            return rtn;
        }
        public static dynamic GetCancellationPolicies()
        {
            using (var db = Database.Open("sqlloco"))
            {
                return db.Query(@"
                    SELECT  C.CancellationPolicyID
                            ,C.CancellationPolicyName
                    FROM    CancellationPolicy As C
                    WHERE   C.LanguageID = @0
                             AND C.CountryID = @1
                ", LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID());
            }
        }
        /// <summary>
        /// Get a string with the name and description of the policy as informative title
        /// </summary>
        /// <param name="policyID"></param>
        /// <returns></returns>
        public static string GetCancellationPolicyTitle(int policyID)
        {
            using (var db = Database.Open("sqlloco")) {
                var policy = db.QuerySingle(@"
                    SELECT *
                    FROM CancellationPolicy
                    WHERE CancellationPolicyID = @0
                        AND LanguageID = @1 AND CountryID = @2
                ", policyID,
                    LcData.GetCurrentLanguageID(),
                    LcData.GetCurrentCountryID());
                return policy.CancellationPolicyName + " (" + policy.CancellationPolicyDescription + ")";
            }
        }
        #endregion

        #region Create Booking Request (Wizard)

        #region SQLs
        /// <summary>
        ///        /* sql example to implement custom auto increment in a secure mode (but with possible deadlocks)
        ///            BEGIN TRAN
        ///                SELECT @id = MAX(id) + 1 FROM Table1 WITH (UPDLOCK, HOLDLOCK)
        ///                INSERT INTO Table1(id, data_field)
        ///                VALUES (@id ,'[blob of data]')
        ///            COMMIT TRAN
        ///         */
        /// </summary>
        private const string sqlInsEstimate = @"
                    BEGIN TRAN

                        -- Getting a new ID if was not provided one
                        DECLARE @id int, @revision int
                        SET @id = @0
                        SET @revision = @1

                        If @id <= 0 BEGIN
                            SELECT @id = MAX(PricingEstimateID) + 1 FROM PricingEstimate WITH (UPDLOCK, HOLDLOCK)
                            SET @revision = 1
                        END

                        IF @id is null 
                            SET @id = 1

                        INSERT INTO [pricingestimate]
                                   ([PricingEstimateID]
                                   ,[PricingEstimateRevision]
                                   ,[ServiceDuration]
                                   ,[FirstSessionDuration]
                                   ,[SubtotalPrice]
                                   ,[FeePrice]
                                   ,[TotalPrice]
                                   ,[PFeePrice]
                                   ,[CreatedDate]
                                   ,[UpdatedDate]
                                   ,[ModifiedBy]
                                   ,[Active])
                             VALUES
                                   (@id, @revision, @2, @3, @4, @5, @6, @7, getdate(), getdate(), 'sys', 1)

                        SELECT @id As PricingEstimateID, @revision As PricingEstimateRevision
                    COMMIT TRAN
        ";
        public const string sqlInsEstimateDetails = @"
                    INSERT INTO [pricingestimatedetail]
                               ([PricingEstimateID]
                               ,[PricingEstimateRevision]

                               ,PricingGroupID

                               ,[ProviderPackageID]
                               ,[ProviderPricingDataInput]
                               ,[CustomerPricingDataInput]

                               ,[ServiceDuration]
                               ,[FirstSessionDuration]
                               ,[HourlyPrice]
                               ,[SubtotalPrice]
                               ,[FeePrice]
                               ,[TotalPrice]

                               ,[CreatedDate]
                               ,[UpdatedDate]
                               ,[ModifiedBy])
                         VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, getdate(), getdate(), 'sys')
        ";
        private const string sqlInsBookingRequest = @"
                INSERT INTO BookingRequest
                           (
                           [CustomerUserID]
                           ,[ProviderUserID]
                           ,[PositionID]
                           ,[BookingTypeID]
                           ,[PricingEstimateID]
                           ,[BookingRequestStatusID]
                           ,[CancellationPolicyID]
                           ,[SpecialRequests]
                           ,[CreatedDate]
                           ,[UpdatedDate]
                           ,[ModifiedBy])
                    VALUES (@0, @1, @2, @3, @4, 1 /* created */, @5, @6, getdate(), getdate(), 'sys')

                -- Update customer user profile to be a customer (if is not still, maybe is only provider)
                UPDATE Users SET IsCustomer = 1
                WHERE UserID = @0 AND IsCustomer <> 1

                SELECT Cast(@@Identity As int) As BookingRequestID
        ";
        #endregion

        /// <summary>
        /// Create an estimate or a revision of the
        /// estimate given at estimateID (if is Zero, create one new).
        /// Returning an object with properties
        /// int:PricingEstimateID and int:PricingEstimateRevision
        /// </summary>
        /// <param name="estimateID"></param>
        /// <param name="revisionID"></param>
        /// <param name="pricingTypeID"></param>
        /// <param name="timeRequired"></param>
        /// <param name="subtotalPrice"></param>
        /// <param name="feePrice"></param>
        /// <param name="totalPrice"></param>
        /// <param name="pfeePrice"></param>
        /// <returns></returns>
        public static dynamic CreatePricingEstimate(
            int estimateID,
            int revisionID,
            decimal timeRequired,
            decimal firstSessionTime,
            decimal subtotalPrice,
            decimal feePrice,
            decimal totalPrice,
            decimal pfeePrice)
        {
            using (var db = Database.Open("sqlloco"))
            {
                return db.QuerySingle(LcData.Booking.sqlInsEstimate, estimateID, revisionID,
                    timeRequired, firstSessionTime, subtotalPrice, feePrice, totalPrice, pfeePrice);
            }
        }

        public static int CreateRequest(
                int customerUserID,
                int providerUserID,
                int positionID,
                int bookingTypeID,
                int pricingEstimateID,
                int cancellationPolicyID,
                string specialRequests)
        {
            int bookingRequestID = 0;
            using (var db = Database.Open("sqlloco"))
            {
                bookingRequestID = (int)db.QueryValue(sqlInsBookingRequest,
                    customerUserID, providerUserID, positionID,
                    bookingTypeID,
                    pricingEstimateID,
                    cancellationPolicyID,
                    specialRequests
                );
            }

            // Saving it at user Session, for other 
            System.Web.HttpContext.Current.Session["BookingRequestID"] = bookingRequestID;

            return bookingRequestID;
        }

        /// <summary>
        /// Get the db fees record that applied on standard bookings, depending on paramenters is a first-time, repeat or book-me fees. 
        /// </summary>
        /// <param name="customerUserID"></param>
        /// <param name="providerUserID"></param>
        /// <param name="pricingTypeID"></param>
        /// <param name="positionID"></param>
        /// <param name="bookCode"></param>
        /// <returns></returns>
        public static dynamic GetFeeFor(int customerUserID, int providerUserID, int pricingTypeID, int positionID, string bookCode = null)
        {
            using (var db = Database.Open("sqlloco"))
            {
                // If there is a book-code
                if (bookCode != null)
                {
                    // Check that match the provider book-code
                    if (null != db.QueryValue(@"
                        SELECT 'found' as A FROM Users 
                        WHERE UserID = @0 AND BookCode like @1
                        ", providerUserID, bookCode))
                    {
                        // Matchs! Use the special Fees record for this cases (ID=7)
                        var codeFees = db.QuerySingle(@"
                            SELECT  *
                            FROM    BookingType
                            WHERE   BookingTypeID = 7
                                     AND Active = 1
                        ");
                        if (codeFees != null)
                        {
                            return codeFees;
                        }
                    }
                    // IF DOES NOT match or there is no fees record or is disabled,
                    // continue with the standard fees information
                }
                // Find fees information row, standard fees:
                // If the customer already booked provider, use the 'repeat booking' fees (ID:2)
                // else use the standard fees (ID:1)
                return db.QuerySingle(@"
                    SELECT  *
                    FROM    BookingType
                    WHERE   BookingTypeID = (
                        SELECT TOP 1 CASE
                            WHEN EXISTS (SELECT * FROM BookingRequest As B 
                                WHERE B.BookingRequestStatusID = 7 AND B.CustomerUserID = @0 AND B.ProviderUserID = @1)
                            THEN 2
                            ELSE 1 END
                    )
                ", customerUserID, providerUserID);
            }
        }
        /// <summary>
        /// Gets the db record with fees amounts that applied on free packages
        /// </summary>
        /// <param name="customerUserID"></param>
        /// <param name="providerUserID"></param>
        /// <param name="pricingTypeID"></param>
        /// <param name="positionID"></param>
        /// <param name="bookCode"></param>
        /// <returns></returns>
        public static dynamic GetFeeForFreePackages(int customerUserID, int providerUserID, int pricingTypeID, int positionID, string bookCode = null)
        {
            using (var db = Database.Open("sqlloco"))
            {
                // Find fees for 'estimate booking' (ID:4), AKA Free package booking (flat fees)
                return db.QuerySingle(@"
                    SELECT  *
                    FROM    BookingType
                    WHERE   BookingTypeID = 4
                ");
            }
        }
        #endregion

        #region Bookings Information Utilities
        public const int BookingRequestConfirmationLimitHours = 18;
        public static string GetBookingTitleFor(int bookingStatusID, dynamic pairUserData, LcData.UserInfo.UserType sentTo)
        {
            var statusTitle = "Booking involving {0}. Unknown status";
            switch (sentTo)
            {
                case LcData.UserInfo.UserType.Provider:
                    statusTitle = "Booking from {0}. Unknown status";
                    switch (bookingStatusID)
                    {
                        case 1: // confirmed
                            statusTitle = "Booking confirmation for {0}";
                            break;
                        case 2: // service performed no pricing adjustment
                            statusTitle = "Service performed by {0}";
                            break;
                        case 3: // service performed pricing adjustment
                            statusTitle = "Service performed by {0} with pricing adjustment";
                            break;
                        case 4: // service performed and paid full
                            statusTitle = "Service completed by {0}";
                            break;
                        case 5: // service dispute
                            statusTitle = "Service dispute with {0}";
                            break;
                        case 6: // cancelled by customer
                            statusTitle = "{0} cancelled your booking";
                            break;
                    }
                    break;
                case LcData.UserInfo.UserType.Customer:
                    statusTitle = "Booking to {0}. Unknown status";
                    switch (bookingStatusID)
                    {
                        case 1: // confirmed
                            statusTitle = "Booking confirmation for {0}";
                            break;
                        case 2: // service performed no pricing adjustment
                            statusTitle = "Service performed by {0}";
                            break;
                        case 3: // service performed pricing adjustment
                            statusTitle = "Service performed by {0} with pricing adjustment";
                            break;
                        case 4: // service performed and paid full
                            statusTitle = "Service completed by {0}";
                            break;
                        case 5: // service dispute
                            statusTitle = "Service dispute with {0}";
                            break;
                        case 6: // cancelled by customer
                            statusTitle = "Cancellation of your booking with {0}";
                            break;
                    }
                    break;
            }
            return String.Format(statusTitle, ASP.LcHelpers.GetUserDisplayName(pairUserData));
        }
        public static string GetBookingRequestTitleFor(int bookingRequestStatusID, dynamic pairUserData, LcData.UserInfo.UserType sentTo)
        {
            var statusTitle = "Booking request involving {0}. Unknown status";
            switch (sentTo)
            {
                case LcData.UserInfo.UserType.Provider:
                    statusTitle = "Booking request for {0}. Unknown status";
                    switch (bookingRequestStatusID)
                    {
                        case 1: // created but not complete
                        case 3: // created but not complete because time out
                            statusTitle = "Incomplete booking request from {0}";
                            break;
                        case 2: // completed request by customer, awaiting provider confirmation
                            statusTitle = "Booking request from {0}";
                            break;
                        case 4: // cancelled by customer
                            statusTitle = "Booking request cancelled by {0}";
                            break;
                        case 5: // denied/declined by provider
                            statusTitle = "You've successfully declined {0}'s booking request";
                            break;
                        case 6: // expired (not answered by provider in time)
                            statusTitle = "Booking request from {0} has expired";
                            break;
                        case 7: // accepted by provider (you must consider view the booking details!)
                            statusTitle = "Accepted booking request from {0}";
                            break;
                        case 8: // denied with alternatives
                            statusTitle = "Declined with alternatives booking request from {0}";
                            break;
                    }
                    break;
                case LcData.UserInfo.UserType.Customer:
                    statusTitle = "Booking request from {0}. Unknown status";
                    switch (bookingRequestStatusID)
                    {
                        case 1: // created but not complete
                        case 3: // created but not complete because time out
                            statusTitle = "Incomplete booking request for {0}";
                            break;
                        case 2: // completed request by customer, awaiting provider confirmation
                            statusTitle = "Booking request for {0}";
                            break;
                        case 4: // cancelled by customer
                            statusTitle = "Booking request for {0} cancelled by me";
                            break;
                        case 5: // denied/declined by provider
                            statusTitle = "{0} is unable to complete your request";
                            break;
                        case 6: // expired (not answered by provider in time)
                            statusTitle = "{0} did not respond to your request";
                            break;
                        case 7: // accepted by provider (you must consider view the booking details!)
                            statusTitle = "Accepted booking request for {0}";
                            break;
                        case 8: // denied with alternatives
                            statusTitle = "Declined with alternatives booking request by {0}";
                            break;
                    }
                    break;
            }
            return String.Format(statusTitle, ASP.LcHelpers.GetUserDisplayName(pairUserData));
        }
        public static string GetUrlPathForBookingRequest(int bookingRequestID)
        {
            return String.Format("dashboard/messages/bookingrequest/{0}", bookingRequestID);
        }
        public static string GetUrlPathForBooking(int bookingID, int bookingRequestID = 0)
        {
            return String.Format("dashboard/messages/booking/{1}", bookingRequestID, bookingID);
        }
        #endregion
    }
}
