using AegisSchedule.Api.Domain;
using AegisSchedule.Api.DTOs;

namespace AegisSchedule.Api.Integrations;

public class SisValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<SisSyncDiagnostic> Errors { get; set; } = [];
    public List<SisSyncDiagnostic> Warnings { get; set; } = [];
}

public static class SisBatchValidator
{
    public static SisValidationResult Validate(SisBatchSyncRequest request)
    {
        var result = new SisValidationResult();

        if (request == null)
        {
            result.Errors.Add(new SisSyncDiagnostic
            {
                Code = "NULL_REQUEST",
                EntityPath = "$",
                Message = "Request payload cannot be empty."
            });
            return result;
        }

        if (request.Term == null)
        {
            result.Errors.Add(new SisSyncDiagnostic
            {
                Code = "INVALID_TERM",
                EntityPath = "Term",
                Message = "Term specification is required."
            });
        }
        else
        {
            if (request.Term.Year <= 0)
            {
                result.Errors.Add(new SisSyncDiagnostic
                {
                    Code = "INVALID_YEAR",
                    EntityPath = "Term.Year",
                    Message = "Year must be a positive integer."
                });
            }

            if (!Enum.IsDefined(typeof(Semester), request.Term.Semester))
            {
                result.Errors.Add(new SisSyncDiagnostic
                {
                    Code = "INVALID_SEMESTER",
                    EntityPath = "Term.Semester",
                    Message = $"Invalid semester '{request.Term.Semester}'."
                });
            }
        }

        if (request.Courses == null || request.Courses.Count == 0)
        {
            result.Warnings.Add(new SisSyncDiagnostic
            {
                Code = "EMPTY_CATALOG",
                EntityPath = "Courses",
                Message = "No courses were provided in the sync payload."
            });
            return result;
        }

        var seenCourseCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int cIdx = 0; cIdx < request.Courses.Count; cIdx++)
        {
            var course = request.Courses[cIdx];
            var coursePath = $"Courses[{cIdx}]";

            if (course == null)
            {
                result.Errors.Add(new SisSyncDiagnostic
                {
                    Code = "NULL_COURSE",
                    EntityPath = coursePath,
                    Message = "Course entry cannot be null."
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(course.Code))
            {
                result.Errors.Add(new SisSyncDiagnostic
                {
                    Code = "EMPTY_COURSE_CODE",
                    EntityPath = $"{coursePath}.Code",
                    Message = "Course code is required."
                });
            }
            else
            {
                coursePath = $"Courses[{course.Code.Trim()}]";
                var trimmedCode = course.Code.Trim();
                if (!seenCourseCodes.Add(trimmedCode))
                {
                    result.Errors.Add(new SisSyncDiagnostic
                    {
                        Code = "DUPLICATE_COURSE_CODE",
                        EntityPath = $"{coursePath}.Code",
                        Message = $"Course code '{trimmedCode}' is duplicated in the batch payload."
                    });
                }
            }

            if (string.IsNullOrWhiteSpace(course.Name))
            {
                result.Errors.Add(new SisSyncDiagnostic
                {
                    Code = "EMPTY_COURSE_NAME",
                    EntityPath = $"{coursePath}.Name",
                    Message = "Course name is required."
                });
            }

            if (course.AcademicLevelNumber <= 0)
            {
                result.Errors.Add(new SisSyncDiagnostic
                {
                    Code = "INVALID_ACADEMIC_LEVEL",
                    EntityPath = $"{coursePath}.AcademicLevelNumber",
                    Message = $"AcademicLevelNumber must be greater than zero. Provided: {course.AcademicLevelNumber}."
                });
            }

            if (course.Activities == null)
            {
                continue;
            }

            var seenActivityTypes = new HashSet<ActivityType>();

            for (int aIdx = 0; aIdx < course.Activities.Count; aIdx++)
            {
                var activity = course.Activities[aIdx];
                var actPath = $"{coursePath}.Activities[{aIdx}]";

                if (activity == null)
                {
                    result.Errors.Add(new SisSyncDiagnostic
                    {
                        Code = "NULL_ACTIVITY",
                        EntityPath = actPath,
                        Message = "Activity entry cannot be null."
                    });
                    continue;
                }

                if (!Enum.IsDefined(typeof(ActivityType), activity.Type))
                {
                    result.Errors.Add(new SisSyncDiagnostic
                    {
                        Code = "INVALID_ACTIVITY_TYPE",
                        EntityPath = $"{actPath}.Type",
                        Message = $"Invalid activity type '{activity.Type}'."
                    });
                }
                else
                {
                    actPath = $"{coursePath}.Activities[{activity.Type}]";
                    if (!seenActivityTypes.Add(activity.Type))
                    {
                        result.Errors.Add(new SisSyncDiagnostic
                        {
                            Code = "DUPLICATE_ACTIVITY_TYPE",
                            EntityPath = actPath,
                            Message = $"Activity of type '{activity.Type}' is duplicated for course '{course.Code}'."
                        });
                    }
                }

                if (activity.Groups == null)
                {
                    continue;
                }

                var seenGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int gIdx = 0; gIdx < activity.Groups.Count; gIdx++)
                {
                    var group = activity.Groups[gIdx];
                    var grpPath = $"{actPath}.Groups[{gIdx}]";

                    if (group == null)
                    {
                        result.Errors.Add(new SisSyncDiagnostic
                        {
                            Code = "NULL_GROUP",
                            EntityPath = grpPath,
                            Message = "Activity group entry cannot be null."
                        });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(group.Name))
                    {
                        result.Errors.Add(new SisSyncDiagnostic
                        {
                            Code = "EMPTY_GROUP_NAME",
                            EntityPath = $"{grpPath}.Name",
                            Message = "Group name is required."
                        });
                    }
                    else
                    {
                        var trimmedGrpName = group.Name.Trim();
                        grpPath = $"{actPath}.Groups[{trimmedGrpName}]";
                        if (!seenGroupNames.Add(trimmedGrpName))
                        {
                            result.Errors.Add(new SisSyncDiagnostic
                            {
                                Code = "DUPLICATE_GROUP_NAME",
                                EntityPath = grpPath,
                                Message = $"Activity group with name '{trimmedGrpName}' is duplicated under activity '{activity.Type}'."
                            });
                        }
                    }

                    if (group.Meetings == null)
                    {
                        continue;
                    }

                    for (int mIdx = 0; mIdx < group.Meetings.Count; mIdx++)
                    {
                        var meeting = group.Meetings[mIdx];
                        var meetingPath = $"{grpPath}.Meetings[{mIdx}]";

                        if (meeting == null)
                        {
                            result.Errors.Add(new SisSyncDiagnostic
                            {
                                Code = "NULL_MEETING",
                                EntityPath = meetingPath,
                                Message = "Meeting entry cannot be null."
                            });
                            continue;
                        }

                        if (!Enum.IsDefined(typeof(DayOfWeek), meeting.DayOfWeek))
                        {
                            result.Errors.Add(new SisSyncDiagnostic
                            {
                                Code = "INVALID_DAY_OF_WEEK",
                                EntityPath = $"{meetingPath}.DayOfWeek",
                                Message = $"Invalid DayOfWeek '{meeting.DayOfWeek}'."
                            });
                        }

                        if (meeting.StartTime >= meeting.EndTime)
                        {
                            result.Errors.Add(new SisSyncDiagnostic
                            {
                                Code = "INVALID_TIME_RANGE",
                                EntityPath = meetingPath,
                                Message = $"StartTime '{meeting.StartTime:HH:mm}' must be strictly earlier than EndTime '{meeting.EndTime:HH:mm}'."
                            });
                        }
                    }

                    // Intra-group overlap check
                    for (int x = 0; x < group.Meetings.Count; x++)
                    {
                        var m1 = group.Meetings[x];
                        if (m1 == null || m1.StartTime >= m1.EndTime) continue;

                        for (int y = x + 1; y < group.Meetings.Count; y++)
                        {
                            var m2 = group.Meetings[y];
                            if (m2 == null || m2.StartTime >= m2.EndTime) continue;

                            if (m1.DayOfWeek == m2.DayOfWeek && m1.StartTime < m2.EndTime && m2.StartTime < m1.EndTime)
                            {
                                result.Errors.Add(new SisSyncDiagnostic
                                {
                                    Code = "INTRA_GROUP_OVERLAP",
                                    EntityPath = $"{grpPath}.Meetings[{x},{y}]",
                                    Message = $"Meetings on {m1.DayOfWeek} ({m1.StartTime:HH:mm}-{m1.EndTime:HH:mm}) and ({m2.StartTime:HH:mm}-{m2.EndTime:HH:mm}) overlap within group '{group.Name}'."
                                });
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
}
