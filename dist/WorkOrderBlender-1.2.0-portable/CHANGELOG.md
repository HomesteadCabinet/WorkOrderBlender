# Changelog

All notable changes to WorkOrderBlender will be documented in this file.

## [1.0.0] - 2024-01-01

### Added
- Initial release of Work Order SDF Consolidator
- Pending changes preview functionality
- Real-time change tracking with count display
- Auto-update functionality via GitHub releases
- Settings dialog for configuration
- Support for editing individual cells in MetricsDialog
- Proper deletion handling without saving all cell values
- In-memory edit store for tracking changes before consolidation

### Features
- Scan work order directories for SDF files
- Select multiple work orders for consolidation
- Preview pending changes before running consolidation
- Edit data in MetricsDialog with real-time updates
- Delete records with proper tracking
- Auto-update checks on startup
- Manual update checks via "Check Updates" button

### Technical Details
- Built with .NET Framework 4.8
- Uses AutoUpdater.NET for update functionality
- SQL Server Compact Edition for database operations
- Custom virtual ListView for performance with large datasets
