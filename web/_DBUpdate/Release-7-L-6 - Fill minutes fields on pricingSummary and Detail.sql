
  
UPDATE PricingSummaryDetail SET
	ServiceDurationMinutes = (cast (round([ServiceDurationHours] * 60, 0) as int)),
	FirstSessionDurationMinutes = (cast (round([FirstSessionDurationHours] * 60, 0) as int))

GO

UPDATE PricingSummary SET
	ServiceDurationMinutes = (cast (round([ServiceDurationHours] * 60, 0) as int)),
	FirstSessionDurationMinutes = (cast (round([FirstSessionDurationHours] * 60, 0) as int))

GO