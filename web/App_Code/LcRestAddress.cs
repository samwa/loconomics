﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebMatrix.Data;

/// <summary>
/// Implements the scheme for a 'service-address' object
/// in the REST API, and static methods for database
/// operations
/// </summary>
public class LcRestAddress
{
    #region Fields
    public int addressID { get; private set; }
    public int jobTitleID { get; private set; }
    public int userID { get; private set; }
    public string addressName;
    public string addressLine1;
    public string addressLine2;
    private int postalCodeID;
    public string postalCode;
    public string city { get; private set; }
    private int stateProvinceID;
    public string stateProvinceCode { get; private set; }
    public string stateProvinceName { get; private set; }
    private int countryID;
    public string countryCode { get; private set; }
    public double? latitude;
    public double? longitude;
    public string specialInstructions;
    public bool isServiceLocation;
    public bool isServiceArea;
    public decimal? serviceRadius;
    public DateTime createdDate { get; private set; }
    public DateTime updatedDate { get; private set; }
    /// <summary>
    /// A valid AddressKind string.
    /// </summary>
    public string kind { get; private set; }
    #endregion

    #region Enums
    public const int NotAJobTitleID = 0;
    /// <summary>
    /// Internal address type, used in database but not exposed
    /// in the REST API.
    /// </summary>
    public enum AddressType : short {
        Home = 1,
        Billing = 13,
        Other = 12
    }
    /// <summary>
    /// Public address kind enumeration (string is not valid as Enum, so static
    /// class with prefixed values per public const property).
    /// Its needed to properly mark and expose each kind of
    /// address that can exists in the API (a bit like the 'special types'
    /// that were inconsistently managed with the AddressType).
    /// Let each returned address to be clearly identified
    /// inside the API. The numeric values are only internal
    /// is expected to expose as text.
    /// </summary>
    public static class AddressKind
    {
        #region Valid Property Values
        public const string Home = "home";
        public const string Billing = "billing";
        public const string Service = "service";
        #endregion
        
        #region Utils
        private static List<string> List = new List<string> { "home", "billing", "service" };
        
        public static bool IsValid(string kind)
        {
            return List.Contains(kind);
        }

        public static string GetFromAddressDBRecord(dynamic address)
        {
            var addressTypeID = (int)address.addressTypeID;
            var jobTitleID = (int)address.jobTitleID;

            // It is attached to a job title (>0), doesn't matters
            // the typeID, is treated ever as a 'service' address.
            if (jobTitleID > 0)
            {
                return Service;
            }
            else
            {
                switch (addressTypeID)
                {
                    case (short)AddressType.Home:
                        return Home;
                    case (short)AddressType.Billing:
                        return Billing;
                    default:
                        // Its really strange to end here (not if the data is consistent)
                        // but any way, under any corrupted data or something, treated
                        // like a service address
                        return Service;
                }
            }
        }
        #endregion
    }
    #endregion

    public static LcRestAddress FromDB(dynamic record)
    {
        if (record == null) return null;
        return new LcRestAddress {
            addressID = record.addressID,
            jobTitleID = record.jobTitleID,
            userID = record.userID,
            addressName = record.addressName,
            addressLine1 = record.addressLine1,
            addressLine2 = record.addressLine2,
            postalCodeID = record.postalCodeID,
            postalCode = record.postalCode,
            city = record.city,
            stateProvinceID = record.stateProvinceID,
            stateProvinceCode = record.stateProvinceCode,
            stateProvinceName = record.stateProvinceName,
            countryID = record.countryID,
            countryCode = record.countryCode,
            latitude = record.latitude,
            longitude = record.longitude,
            specialInstructions = record.specialInstructions,
            isServiceLocation = record.isServiceLocation,
            isServiceArea = record.isServiceArea,
            serviceRadius = N.D(record.serviceRadius) == null ? null : DataTypes.GetTypedValue<decimal?>(record.serviceRadius, 0),
            createdDate = record.createdDate,
            updatedDate = record.updatedDate,
            kind = AddressKind.GetFromAddressDBRecord(record)
        };
    }

    #region SQL
    private static string sqlSelect = @"SELECT ";
    private static string sqlSelectOne = @"SELECT TOP 1 ";
    private static string sqlFields = @"
                L.AddressID as addressID
                ,L.UserID as userID
                ,coalesce(SA.PositionID, 0) as jobTitleID
                ,L.AddressName as addressName
                ,L.AddressLine1 as addressLine1
                ,L.AddressLine2 as addressLine2

                ,L.PostalCodeID as postalCodeID
                ,PC.PostalCode as postalCode
                ,L.City as city
                ,L.StateProvinceID as stateProvinceID
                ,SP.StateProvinceCode as stateProvinceCode
                ,SP.StateProvinceName as stateProvinceName
                ,L.CountryID as countryID
                ,C.CountryCodeAlpha2 as countryCode

                ,L.Latitude as latitude
                ,L.Longitude as longitude
                ,L.SpecialInstructions as specialInstructions

                ,coalesce(SA.ServicesPerformedAtLocation, Cast(0 as bit)) as isServiceLocation
                ,coalesce(SA.TravelFromLocation, Cast(0 as bit)) as isServiceArea
                ,SA.ServiceRadiusFromLocation as serviceRadius

                ,L.CreatedDate as createdDate
                ,CASE WHEN SA.UpdatedDate is null OR L.UpdatedDate > SA.UpdatedDate THEN L.UpdatedDate ELSE SA.UpdatedDate END as updatedDate

                ,L.AddressTypeID as addressTypeID

        FROM    Address As L
                 INNER JOIN
                StateProvince As SP
                  ON L.StateProvinceID = SP.StateProvinceID
                 INNER JOIN
                PostalCode As PC
                  ON PC.PostalCodeID = L.PostalCodeID
                 INNER JOIN
                Country As C
                  ON L.CountryID = C.CountryID
                    AND C.LanguageID = @0
                 LEFT JOIN
                ServiceAddress As SA
                  -- Special case when the jobtitle/position requested is zero
                  -- just dont let make the relation to avoid bad results
                  -- because of internally reused addressID.
                  ON @2 > 0 AND L.AddressID = SA.AddressID
        WHERE   L.Active = 1
                 AND L.UserID = @1
    ";
    private static string sqlAndJobTitleID = @"
        AND coalesce(SA.PositionID, 0) = @2
    ";
    private static string sqlAndAddressID = @"
        AND L.AddressID = @3
    ";
    private static string sqlAndTypeID = @"
        AND L.AddressTypeID = @4
    ";
    #endregion

    #region Fetch
    public static List<LcRestAddress> GetServiceAddresses(int userID, int jobTitleID)
    {
        using (var db = Database.Open("sqlloco"))
        {
            return db.Query(sqlSelect + sqlFields + sqlAndJobTitleID,
                LcData.GetCurrentLanguageID(), userID, jobTitleID)
                .Select(FromDB)
                .ToList();
        }
    }

    public static List<LcRestAddress> GetBillingAddresses(int userID)
    {
        using (var db = Database.Open("sqlloco"))
        {
            // Parameter jobTitleID needs to be specified as 0 to avoid to join
            // the service-address table
            // Null value as 3th parameter since that placeholder is reserved for addressID
            return db.Query(sqlSelect + sqlFields + sqlAndJobTitleID + sqlAndTypeID,
                LcData.GetCurrentLanguageID(), userID, NotAJobTitleID, null, AddressType.Billing)
                .Select(FromDB)
                .ToList();
        }
    }

    private static LcRestAddress GetSingleFrom(IEnumerable<dynamic> dbRecords)
    {
        var add = dbRecords
            .Select(FromDB)
            .ToList();

        if (add.Count == 0)
            return null;
        else
            return add[0];
    }
    
    public static LcRestAddress GetServiceAddress(int userID, int jobTitleID, int addressID)
    {
        using (var db = Database.Open("sqlloco"))
        {
            return GetSingleFrom(db.Query(
                sqlSelectOne + sqlFields + sqlAndJobTitleID + sqlAndAddressID,
                LcData.GetCurrentLanguageID(), userID, jobTitleID, addressID
            ));
        }
    }

    public static LcRestAddress GetBillingAddress(int userID, int addressID)
    {
        using (var db = Database.Open("sqlloco"))
        {
            // Parameter jobTitleID needs to be specified as 0 to avoid to join
            // the service-address table
            return GetSingleFrom(db.Query(
                sqlSelectOne + sqlFields + sqlAndJobTitleID + sqlAndAddressID + sqlAndTypeID,
                LcData.GetCurrentLanguageID(), userID, NotAJobTitleID, addressID, AddressType.Billing
            ));
        }
    }

    public static LcRestAddress GetHomeAddress(int userID)
    {
        using (var db = Database.Open("sqlloco"))
        {
            // Parameter jobTitleID needs to be specified as 0 to avoid to join
            // the service-address table
            // Null value as 3th parameter since that placeholder is reserved for addressID
            // NOTE: Home address must exists ever, created on sign-up (GetSingleFrom
            // takes care to return null if not exists, but on this case is not possible
            // --or must not if not corrupted user profile)
            return GetSingleFrom(db.Query(
                sqlSelectOne + sqlFields + sqlAndJobTitleID + sqlAndTypeID,
                LcData.GetCurrentLanguageID(), userID, NotAJobTitleID, null, AddressType.Home
            ));
        }
    }
    #endregion
}