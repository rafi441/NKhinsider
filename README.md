# NKhinsider

A modern WPF application for downloading video game music soundtracks from [Khinsider](https://downloads.khinsider.com/). Built with Material Design UI components for a clean and intuitive user experience.

## Features

- **🎵 Bulk Album Downloads**: Download entire video game soundtrack albums with a single click
- **🎨 Modern Material Design UI**: Beautiful, responsive interface with dark/light theme support
- **⚡ Parallel Downloads**: Fast concurrent downloading (up to 5 tracks simultaneously)
- **📁 Smart File Organization**: Automatically creates organized album folders
- **📊 Real-time Progress Tracking**: Visual progress bars and detailed logging
- **🛡️ Error Handling**: Robust error handling with retry mechanisms
- **🔗 URL Validation**: Automatic validation of Khinsider album URLs
- **📋 Detailed Logging**: Comprehensive download logs with different severity levels
- **⏹️ Download Control**: Start, stop, and cancel downloads at any time
- **🖼️ Album Artwork**: Displays album cover images when available

## Requirements

- **OS**: Windows 10/11
- **.NET**: .NET 8.0 or later
- **Internet**: Active internet connection for downloading

## Usage

1. **Launch the Application**: Run `NKhinsider.exe`

2. **Enter Album URL**: 
   - Navigate to [Khinsider](https://downloads.khinsider.com/)
   - Find the video game soundtrack you want to download
   - Copy the album URL (should look like: `https://downloads.khinsider.com/game-soundtracks/album/...`)
   - Paste it into the URL field in the application

3. **Select Download Location**:
   - Click "Browse Folder" to choose where you want to save the music
   - Default location is your Music folder

4. **Start Download**:
   - Click the "Download" button
   - The application will fetch album information and display it
   - Tracks will be downloaded in parallel for faster completion

5. **Monitor Progress**:
   - Watch the progress bar and track counter
   - View detailed logs in the right panel
   - Use the "Stop" button to cancel if needed

## Features in Detail

### Smart Download Management
- **Parallel Processing**: Downloads up to 5 tracks simultaneously for maximum speed
- **Resume Capability**: Skips already downloaded files automatically
- **Error Recovery**: Continues downloading other tracks if one fails
- **Cancellation**: Clean cancellation that stops all active downloads

### File Organization
- Creates album-specific folders with clean names
- Removes invalid characters from file and folder names
- Preserves original track names and order
- Automatically opens the download folder when complete

### Logging System
- **Info**: General download progress and status updates
- **Success**: Completed downloads and successful operations
- **Warning**: Non-critical issues and user cancellations
- **Error**: Failed downloads and critical errors
- **Debug**: Technical details for troubleshooting

## Dependencies

The application uses the following NuGet packages:

- **MaterialDesignThemes** (4.9.0): Modern Material Design UI components
- **MaterialDesignColors** (2.1.4): Color theming for Material Design
- **HtmlAgilityPack** (1.11.54): HTML parsing for web scraping
- **Microsoft-WindowsAPICodePack-Shell** (1.1.5): Native Windows folder browser dialog

## Architecture

The application follows the MVVM (Model-View-ViewModel) pattern:

- **`MainWindow.xaml`**: UI layout and styling
- **`KhinsiderViewModel.cs`**: Data binding and UI state management
- **`KhinsiderService.cs`**: Core download logic and web scraping
- **`LogEntry.cs`**: Logging data model

## Technical Details

### Web Scraping
- Uses proper User-Agent headers to avoid blocking
- Parses HTML tables to extract track information
- Handles both direct MP3 links and song page redirects
- Robust error handling for network issues

### Download Engine
- HTTP client with configurable timeouts
- Progress reporting for individual and batch downloads
- Concurrent download limiting to prevent server overload
- Automatic retry logic for failed downloads

## Troubleshooting

### Common Issues

**"Invalid URL" Error**
- Ensure you're using a Khinsider album URL
- URL should contain `/game-soundtracks/album/`

**Downloads Failing**
- Check your internet connection
- Some albums may have broken links on Khinsider
- Try downloading individual tracks instead

**Permission Errors**
- Run as administrator if downloading to system folders
- Choose a different download location (like Desktop or Documents)

**Application Won't Start**
- Ensure .NET 8.0 Runtime is installed
- Check Windows compatibility (Windows 10/11 required)

### Development Setup
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Disclaimer

This application is for personal use only. Please respect the terms of service of Khinsider and only download music you have the right to download. The developers are not responsible for any misuse of this software.

---

⭐ **If you find this project useful, please consider giving it a star!** ⭐
