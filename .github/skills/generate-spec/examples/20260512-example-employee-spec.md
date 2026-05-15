# Employee CRUD

## 1. Feature Summary

The HR system needs a way to manage employee records through a web interface. HR administrators will use this feature to create, view, update, and delete employee information including personal details, contact information, and employment status. This replaces the current manual spreadsheet process and provides a centralized source of truth for employee data.

## 2. Data Model / Entities

### Employee
- Name: Full legal name of the employee
- Email: Work email address
- National ID: Government-issued identification number
- Phone: Contact phone number with country code
- Country: Country of residence
- Gender: Gender identity
- Date of Birth: Employee's date of birth
- Official Title: Job title or role
- Hire Date: Date the employee joined the company

## 3. Business Rules & Constraints

The system MUST enforce the following non-negotiable rules:

1. Email addresses must be unique across all employees
2. National IDs must be unique within the same Country (two employees from different countries can have the same National ID format)
3. Phone numbers must include a country code (e.g., +1, +506)
4. Email must be validated as a properly formatted email address
5. Date of Birth and Hire Date must be valid dates in the past
6. All fields are required except Gender (which is optional)

## 4. Acceptance Criteria

The feature is complete when:

- [ ] An HR admin can create a new employee with all required fields
- [ ] An HR admin can view a list of all employees in a grid
- [ ] An HR admin can view a single employee's full details
- [ ] An HR admin can update an existing employee's information
- [ ] An HR admin can delete an employee (with confirmation)
- [ ] The system rejects duplicate email addresses with a clear error message
- [ ] The system rejects duplicate National IDs within the same country
- [ ] The system rejects phone numbers without a country code
- [ ] The system rejects invalid email formats
- [ ] The system rejects invalid or future dates
- [ ] All validation errors are displayed clearly in the UI
- [ ] Unit tests verify all business rules

## 5. Out of Scope

The following are explicitly NOT part of this feature:

- User authentication or authorization
- Employee performance reviews or ratings
- Salary or compensation information
- Document uploads (resumes, contracts, etc.)
- Email notifications when employees are created/updated
- Audit logging of who made changes
- Data export to CSV or other formats
- Advanced search or filtering beyond basic grid sorting

## 6. Open Questions

Decisions needed before implementation:

1. **Should we soft-delete employees or hard-delete them?**
   - Context: If an employee leaves the company, should their record be marked as inactive or permanently removed?
   - Options: Soft delete (add "isActive" flag) or hard delete (remove from database)

2. **Can admins edit an employee's National ID after creation?**
   - Context: National IDs rarely change, but there might be data entry errors
   - Options: Allow editing, or make it read-only after creation

3. **What happens if an admin tries to delete an employee who is assigned to projects?**
   - Context: This won't be relevant until Session 2 when we add Projects
   - Options: Block deletion, allow deletion and remove assignments, or flag as deferred decision