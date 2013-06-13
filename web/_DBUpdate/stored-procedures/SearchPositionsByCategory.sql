/****** Object:  StoredProcedure [dbo].[SearchPositionsByCategory]    Script Date: 06/13/2013 20:45:33 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Iago Lorenzo Salgueiro
-- Create date: 2013-01-03
-- Description:	Get the list of positions 
-- inside the CategoryID given, for categorized
-- search results page
-- =============================================
ALTER PROCEDURE [dbo].[SearchPositionsByCategory]
	@LanguageID int
	,@CountryID int
	,@Category nvarchar(400)
	,@City nvarchar(400)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	DECLARE @ServiceCategoryID AS INT

	SELECT @ServiceCategoryID = ServiceCategoryID 
	FROM servicecategory 
	WHERE Name = @Category
		AND LanguageID = @LanguageID 
		AND CountryID = @CountryID

    SELECT	P.PositionID
			,P.PositionPlural
			,P.PositionSingular
			,P.PositionDescription
			,P.PositionSearchDescription
			
			,avg( (coalesce(UR.Rating1, 0) + coalesce(UR.Rating2, 0) + coalesce(ur.Rating3, 0)) / 3) As AverageRating
			,sum(ur.TotalRatings) As TotalRatings
			,avg(US.ResponseTimeMinutes) As AverageResponseTimeMinutes
			,avg(PHR.HourlyRate) As AverageHourlyRate
			,count(UP.UserID) As ProvidersCount
			
	FROM	Positions As P
			 INNER JOIN
			ServiceCategoryPosition As SCP
			  ON P.PositionID = SCP.PositionID
				AND P.LanguageID = SCP.LanguageID
				AND P.CountryID = SCP.CountryID
				
			 LEFT JOIN
			UserProfilePositions As UP
			  ON UP.PositionID = P.PositionID
			    AND UP.LanguageID = P.LanguageID
			    AND UP.CountryID = P.CountryID
			    AND UP.Active = 1
			    AND UP.StatusID = 1
			 LEFT JOIN
			UserReviewScores AS UR
			  ON UR.UserID = UP.UserID
				AND UR.PositionID = UP.PositionID
			 LEFT JOIN
			UserStats As US
			  ON US.UserID = UP.UserID
			 LEFT JOIN
			ProviderHourlyRate AS PHR
			  ON PHR.UserID = UP.UserID
			    AND PHR.PositionID = UP.PositionID
			    AND PHR.Active = 1
	WHERE
			SCP.ServiceCategoryID = @ServiceCategoryID
			 AND
			SCP.Active = 1
			 AND
			P.Active = 1
			 AND
			P.LanguageID = @LanguageID
			 AND
			P.CountryID = @CountryID
	GROUP BY P.PositionID, P.PositionPlural, P.PositionSingular, P.PositionDescription, P.PositionSearchDescription
END
