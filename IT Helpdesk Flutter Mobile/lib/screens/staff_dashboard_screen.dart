import 'package:flutter/material.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

import '../core/theme/app_theme.dart';
import '../services/ticket_service.dart';
import '../widgets/staff_bottom_nav.dart';

import 'login_screen.dart';
import 'notifications_screen.dart';
import 'profile_screen.dart';

class StaffDashboardScreen extends StatefulWidget {
  final int initialPage;

  const StaffDashboardScreen({super.key, this.initialPage = 0});

  @override
  State<StaffDashboardScreen> createState() => _StaffDashboardScreenState();
}

class _StaffDashboardScreenState extends State<StaffDashboardScreen> {
  final TicketService _service = TicketService();
  final SupabaseClient _supabase = Supabase.instance.client;

  static const Color navy = AppTheme.navy;
  static const Color blue = AppTheme.blue;
  static const Color background = Color(0xFFF5F7FB);
  static const Color muted = Color(0xFF64748B);
  static const Color borderColor = Color(0xFFE2E8F0);

  List<Map<String, dynamic>> _tickets = [];
  List<Map<String, dynamic>> _categories = [];

  bool _loading = true;
  bool _submitting = false;
  String? _error;

  int? _selectedCategory;
  String _priority = 'Medium';
  late int _page;

  @override
  void initState() {
    super.initState();
    _page = widget.initialPage.clamp(0, 2);
    _loadData();
  }

  Future<void> _loadData() async {
    if (!mounted) return;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final tickets = await _service.getMyTickets();
      final categories = await _service.getCategories();

      if (!mounted) return;

      setState(() {
        _tickets = tickets;
        _categories = categories;

        if (_categories.isEmpty) {
          _selectedCategory = null;
        } else if (!_categories.any(
          (category) => (category['id'] as num).toInt() == _selectedCategory,
        )) {
          _selectedCategory = (_categories.first['id'] as num).toInt();
        }
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _error = e.toString());
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _submitTicket() async {
    if (_selectedCategory == null || _submitting) return;

    final category = _categories.firstWhere(
      (item) => (item['id'] as num).toInt() == _selectedCategory,
    );

    setState(() => _submitting = true);

    try {
      await _service.createTicket(
        categoryId: _selectedCategory!,
        categoryName: category['name'].toString(),
        priority: _priority,
      );

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Ticket submitted successfully!'),
          backgroundColor: Color(0xFF16A34A),
        ),
      );

      setState(() {
        _page = 1;
        _priority = 'Medium';
      });

      await _loadData();
    } catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Unable to submit ticket: $e'),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _submitting = false);
      }
    }
  }

  Future<void> _logout() async {
    try {
      await _supabase.auth.signOut();

      if (!mounted) return;

      Navigator.pushAndRemoveUntil(
        context,
        MaterialPageRoute(builder: (_) => const LoginScreen()),
        (_) => false,
      );
    } catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Logout failed: $e')));
    }
  }

  void _openNotifications() {
    Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => const NotificationsScreen()),
    );
  }

  void _openProfile() {
    Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => const ProfileScreen()),
    );
  }

  void _changePage(int index) {
    if (index < 0 || index > 2) return;
    setState(() => _page = index);
  }

  int _countStatus(String status) {
    return _tickets.where((ticket) {
      return ticket['status']?.toString().trim().toLowerCase() ==
          status.toLowerCase();
    }).length;
  }

  Color _statusColor(String status) {
    switch (status.toLowerCase()) {
      case 'open':
        return const Color(0xFFF59E0B);
      case 'in progress':
        return blue;
      case 'resolved':
        return const Color(0xFF16A34A);
      case 'closed':
        return muted;
      default:
        return muted;
    }
  }

  Color _priorityColor(String priority) {
    switch (priority.toLowerCase()) {
      case 'low':
        return const Color(0xFF16A34A);
      case 'medium':
        return const Color(0xFFF59E0B);
      case 'high':
        return const Color(0xFFDC2626);
      default:
        return muted;
    }
  }

  Widget _pageWidth({required Widget child, double maxWidth = 950}) {
    return Center(
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: maxWidth),
        child: child,
      ),
    );
  }

  Widget _sectionTitle(String title) {
    return Text(
      title,
      style: const TextStyle(
        fontSize: 19,
        fontWeight: FontWeight.bold,
        color: navy,
      ),
    );
  }

  Widget _statCard({
    required String title,
    required int count,
    required IconData icon,
    required Color color,
  }) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(17),
        border: Border.all(color: borderColor),
        boxShadow: const [
          BoxShadow(
            color: Color(0x08000000),
            blurRadius: 8,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            padding: const EdgeInsets.all(9),
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.10),
              borderRadius: BorderRadius.circular(11),
            ),
            child: Icon(icon, color: color, size: 21),
          ),
          const SizedBox(height: 10),
          Text(
            '$count',
            style: const TextStyle(
              fontSize: 25,
              fontWeight: FontWeight.bold,
              color: navy,
            ),
          ),
          const SizedBox(height: 3),
          Text(
            title,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: muted, fontSize: 12),
          ),
        ],
      ),
    );
  }

  Widget _quickAccessTile({
    required IconData icon,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Container(
          padding: const EdgeInsets.all(15),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: borderColor),
          ),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(11),
                decoration: BoxDecoration(
                  color: const Color(0xFFEFF6FF),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(icon, color: blue, size: 23),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        color: navy,
                        fontWeight: FontWeight.bold,
                        fontSize: 15,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      subtitle,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(color: muted, fontSize: 12),
                    ),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right, color: muted),
            ],
          ),
        ),
      ),
    );
  }

  // HOME DASHBOARD

  Widget _dashboard() {
    final user = _supabase.auth.currentUser;
    final email = user?.email ?? '';

    final fullName = user?.userMetadata?['full_name']?.toString() ?? '';

    final firstName = fullName.trim().isNotEmpty
        ? fullName.trim().split(' ').first
        : 'Staff Member';

    return RefreshIndicator(
      onRefresh: _loadData,
      child: _pageWidth(
        child: ListView(
          padding: const EdgeInsets.all(18),
          children: [
            Container(
              padding: const EdgeInsets.all(22),
              decoration: BoxDecoration(
                color: navy,
                borderRadius: BorderRadius.circular(20),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Row(
                    children: [
                      Icon(Icons.support_agent, color: Colors.white, size: 27),
                      SizedBox(width: 10),
                      Text(
                        'IT HELPDESK',
                        style: TextStyle(
                          color: Colors.white,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 1.1,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 20),
                  Text(
                    'Hello, $firstName!',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 25,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 6),
                  const Text(
                    'How can we help you today?',
                    style: TextStyle(color: Color(0xFFCBD5E1)),
                  ),
                  const SizedBox(height: 18),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: () => _changePage(2),
                      icon: const Icon(Icons.add_circle_outline),
                      label: const Text('Report an IT Issue'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: blue,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 15),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 26),
            _sectionTitle('Ticket Overview'),
            const SizedBox(height: 14),

            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              mainAxisSpacing: 12,
              crossAxisSpacing: 12,
              mainAxisExtent: 145,
              children: [
                _statCard(
                  title: 'Total Tickets',
                  count: _tickets.length,
                  icon: Icons.confirmation_number_outlined,
                  color: blue,
                ),
                _statCard(
                  title: 'Open',
                  count: _countStatus('Open'),
                  icon: Icons.pending_actions_outlined,
                  color: const Color(0xFFF59E0B),
                ),
                _statCard(
                  title: 'In Progress',
                  count: _countStatus('In Progress'),
                  icon: Icons.build_circle_outlined,
                  color: blue,
                ),
                _statCard(
                  title: 'Resolved',
                  count: _countStatus('Resolved'),
                  icon: Icons.check_circle_outline,
                  color: const Color(0xFF16A34A),
                ),
              ],
            ),

            const SizedBox(height: 26),
            _sectionTitle('Quick Access'),
            const SizedBox(height: 12),

            _quickAccessTile(
              icon: Icons.list_alt_outlined,
              title: 'My Tickets',
              subtitle: 'Track your submitted requests',
              onTap: () => _changePage(1),
            ),

            const SizedBox(height: 10),

            _quickAccessTile(
              icon: Icons.notifications_outlined,
              title: 'Notifications',
              subtitle: 'View your latest ticket updates',
              onTap: _openNotifications,
            ),

            const SizedBox(height: 10),

            _quickAccessTile(
              icon: Icons.person_outline,
              title: 'My Profile',
              subtitle: email,
              onTap: _openProfile,
            ),

            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }

  // CATEGORY AND PRIORITY SELECTION

  Widget _selectionCard({
    required String title,
    required String subtitle,
    required IconData icon,
    required bool selected,
    required VoidCallback onTap,
    Color accent = blue,
  }) {
    return Material(
      color: selected ? const Color(0xFFEFF6FF) : Colors.white,
      borderRadius: BorderRadius.circular(15),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(15),
        child: Container(
          padding: const EdgeInsets.all(15),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(15),
            border: Border.all(
              color: selected ? blue : borderColor,
              width: selected ? 1.7 : 1,
            ),
          ),
          child: Row(
            children: [
              Icon(icon, color: selected ? blue : accent, size: 25),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        color: navy,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      subtitle,
                      style: const TextStyle(color: muted, fontSize: 12),
                    ),
                  ],
                ),
              ),
              Icon(
                selected
                    ? Icons.radio_button_checked
                    : Icons.radio_button_unchecked,
                color: selected ? blue : muted,
              ),
            ],
          ),
        ),
      ),
    );
  }

  // REPORT AN IT ISSUE

  Widget _createTicket() {
    return _pageWidth(
      maxWidth: 750,
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const Text(
            'Report an IT Issue',
            style: TextStyle(
              fontSize: 25,
              fontWeight: FontWeight.bold,
              color: navy,
            ),
          ),

          const SizedBox(height: 8),

          const Text(
            'Select the problem and how urgent it is.',
            style: TextStyle(color: muted),
          ),

          const SizedBox(height: 26),

          _sectionTitle('Select Issue Category'),

          const SizedBox(height: 12),

          if (_categories.isEmpty)
            const Padding(
              padding: EdgeInsets.all(16),
              child: Text('No categories available.'),
            ),

          ..._categories.map((category) {
            final id = (category['id'] as num).toInt();

            return Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: _selectionCard(
                title: category['name'].toString(),
                subtitle: 'IT support request',
                icon: Icons.computer_outlined,
                selected: _selectedCategory == id,
                onTap: () {
                  setState(() {
                    _selectedCategory = id;
                  });
                },
              ),
            );
          }),

          const SizedBox(height: 22),

          _sectionTitle('Select Priority'),

          const SizedBox(height: 8),

          const Text(
            'How urgent is your issue?',
            style: TextStyle(color: muted),
          ),

          const SizedBox(height: 14),

          ...['Low', 'Medium', 'High'].map((priority) {
            return Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: _selectionCard(
                title: priority,
                subtitle: priority == 'Low'
                    ? 'Not urgent'
                    : priority == 'Medium'
                    ? 'Normal priority'
                    : 'Requires urgent attention',
                icon: priority == 'High'
                    ? Icons.priority_high
                    : priority == 'Medium'
                    ? Icons.schedule
                    : Icons.low_priority,
                selected: _priority == priority,
                accent: _priorityColor(priority),
                onTap: () {
                  setState(() {
                    _priority = priority;
                  });
                },
              ),
            );
          }),

          const SizedBox(height: 24),

          SizedBox(
            height: 54,
            child: ElevatedButton.icon(
              onPressed: _submitting || _selectedCategory == null
                  ? null
                  : _submitTicket,
              icon: _submitting
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.send_outlined),
              label: Text(_submitting ? 'Submitting...' : 'Submit Ticket'),
              style: ElevatedButton.styleFrom(
                backgroundColor: blue,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
              ),
            ),
          ),

          const SizedBox(height: 24),
        ],
      ),
    );
  }

  // TICKET STATUS BADGE

  Widget _statusBadge(String status) {
    final color = _statusColor(status);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        status,
        style: TextStyle(
          color: color,
          fontWeight: FontWeight.w600,
          fontSize: 12,
        ),
      ),
    );
  }

  // INDIVIDUAL TICKET CARD

  Widget _ticketCard(Map<String, dynamic> ticket) {
    final status = ticket['status']?.toString() ?? 'Open';

    final priority = ticket['priority']?.toString() ?? 'Medium';

    final reference =
        ticket['referenceNumber'] ?? ticket['reference_number'] ?? 'Pending';

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(17),
        border: Border.all(color: borderColor),
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          borderRadius: BorderRadius.circular(17),
          onTap: () => _showTicketDetails(ticket),
          child: Padding(
            padding: const EdgeInsets.all(17),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        reference.toString(),
                        style: const TextStyle(
                          color: blue,
                          fontWeight: FontWeight.bold,
                          fontSize: 13,
                        ),
                      ),
                    ),
                    _statusBadge(status),
                  ],
                ),

                const SizedBox(height: 13),

                Text(
                  ticket['title']?.toString() ?? 'IT Issue',
                  style: const TextStyle(
                    color: navy,
                    fontWeight: FontWeight.bold,
                    fontSize: 17,
                  ),
                ),

                const SizedBox(height: 14),

                Row(
                  children: [
                    Icon(
                      Icons.flag_outlined,
                      color: _priorityColor(priority),
                      size: 17,
                    ),

                    const SizedBox(width: 6),

                    Text(
                      '$priority Priority',
                      style: const TextStyle(color: muted, fontSize: 13),
                    ),

                    const Spacer(),

                    const Text(
                      'View Details',
                      style: TextStyle(
                        color: blue,
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                      ),
                    ),

                    const Icon(Icons.chevron_right, color: blue, size: 19),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  // MY TICKETS PAGE

  Widget _myTickets() {
    return RefreshIndicator(
      onRefresh: _loadData,
      child: _pageWidth(
        maxWidth: 850,
        child: ListView(
          padding: const EdgeInsets.all(18),
          children: [
            _sectionTitle('My Tickets'),

            const SizedBox(height: 7),

            const Text(
              'Track the progress of your IT requests.',
              style: TextStyle(color: muted),
            ),

            const SizedBox(height: 20),

            if (_tickets.isEmpty) ...[
              const SizedBox(height: 80),

              const Icon(Icons.inbox_outlined, color: muted, size: 64),

              const SizedBox(height: 14),

              const Center(
                child: Text(
                  'No tickets submitted yet.',
                  style: TextStyle(color: muted),
                ),
              ),

              const SizedBox(height: 20),

              ElevatedButton.icon(
                onPressed: () => _changePage(2),
                icon: const Icon(Icons.add),
                label: const Text('Create Your First Ticket'),
              ),
            ] else
              ..._tickets.map(_ticketCard),
          ],
        ),
      ),
    );
  }

  // TICKET DETAILS BOTTOM SHEET

  void _showTicketDetails(Map<String, dynamic> ticket) {
    final reference =
        ticket['referenceNumber'] ?? ticket['reference_number'] ?? 'Pending';

    final status = ticket['status']?.toString() ?? 'Open';

    final priority = ticket['priority']?.toString() ?? 'Medium';

    final resolution = ticket['resolutionNotes'] ?? ticket['resolution_notes'];

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (sheetContext) {
        return SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _sectionTitle('Ticket Details'),

                const SizedBox(height: 20),

                Text(
                  reference.toString(),
                  style: const TextStyle(
                    color: blue,
                    fontWeight: FontWeight.bold,
                  ),
                ),

                const SizedBox(height: 16),

                Text(
                  ticket['title']?.toString() ?? 'IT Issue',
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                    color: navy,
                  ),
                ),

                const SizedBox(height: 20),

                const Text('Status', style: TextStyle(color: muted)),

                const SizedBox(height: 6),

                _statusBadge(status),

                const SizedBox(height: 18),

                const Text('Priority', style: TextStyle(color: muted)),

                const SizedBox(height: 6),

                Text(
                  priority,
                  style: TextStyle(
                    color: _priorityColor(priority),
                    fontWeight: FontWeight.bold,
                  ),
                ),

                if (resolution != null &&
                    resolution.toString().trim().isNotEmpty) ...[
                  const SizedBox(height: 18),

                  const Text(
                    'Resolution Notes',
                    style: TextStyle(color: muted),
                  ),

                  const SizedBox(height: 6),

                  Text(resolution.toString()),
                ],

                const SizedBox(height: 28),

                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton(
                    onPressed: () => Navigator.pop(sheetContext),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: blue,
                      foregroundColor: Colors.white,
                    ),
                    child: const Text('Close'),
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  // MAIN SCREEN

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: background,

      appBar: AppBar(
        backgroundColor: navy,
        foregroundColor: Colors.white,
        elevation: 0,
        title: Text(
          ['Dashboard', 'My Tickets', 'Report Issue'][_page],
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        actions: [
          IconButton(
            tooltip: 'Notifications',
            icon: const Icon(Icons.notifications_outlined),
            onPressed: _openNotifications,
          ),

          IconButton(
            tooltip: 'My Profile',
            icon: const Icon(Icons.person_outline),
            onPressed: _openProfile,
          ),

          IconButton(
            tooltip: 'Refresh',
            icon: const Icon(Icons.refresh),
            onPressed: _loadData,
          ),

          PopupMenuButton<String>(
            tooltip: 'More options',
            onSelected: (value) {
              if (value == 'logout') {
                _logout();
              }
            },
            itemBuilder: (context) => const [
              PopupMenuItem(
                value: 'logout',
                child: Row(
                  children: [
                    Icon(Icons.logout),
                    SizedBox(width: 10),
                    Text('Logout'),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),

      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(
                      Icons.error_outline,
                      color: Colors.red,
                      size: 48,
                    ),

                    const SizedBox(height: 12),

                    Text(_error!, textAlign: TextAlign.center),

                    const SizedBox(height: 16),

                    ElevatedButton(
                      onPressed: _loadData,
                      child: const Text('Retry'),
                    ),
                  ],
                ),
              ),
            )
          : [_dashboard(), _myTickets(), _createTicket()][_page],

      bottomNavigationBar: StaffBottomNav(
        selectedIndex: _page,
        onDestinationSelected: _changePage,
      ),
    );
  }
}
