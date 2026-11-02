import 'package:flutter/material.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

import '../core/theme/app_theme.dart';
import '../widgets/staff_bottom_nav.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  final _supabase = Supabase.instance.client;

  static const Color navy = AppTheme.navy;
  static const Color blue = AppTheme.blue;
  static const Color background = Color(0xFFF5F7FB);
  static const Color muted = Color(0xFF64748B);

  late Future<List<Map<String, dynamic>>> _notifications;

  bool _showUnreadOnly = false;

  final Set<String> _updatingIds = {};

  @override
  void initState() {
    super.initState();
    _notifications = _fetchNotifications();
  }

  Future<List<Map<String, dynamic>>> _fetchNotifications() async {
    final userId = _supabase.auth.currentUser?.id;

    if (userId == null) return [];

    final data = await _supabase
        .from('notifications')
        .select('id, title, message, is_read, created_at')
        .eq('user_id', userId)
        .order('created_at', ascending: false);

    return List<Map<String, dynamic>>.from(data);
  }

  Future<void> _reload() async {
    final future = _fetchNotifications();

    setState(() {
      _notifications = future;
    });

    try {
      await future;
    } catch (_) {
      // FutureBuilder handles the error.
    }
  }

  Future<void> _markAsRead(dynamic id) async {
    final userId = _supabase.auth.currentUser?.id;

    if (userId == null || id == null) return;

    final notificationId = id.toString();

    if (_updatingIds.contains(notificationId)) return;

    setState(() {
      _updatingIds.add(notificationId);
    });

    try {
      await _supabase
          .from('notifications')
          .update({'is_read': true})
          .eq('id', id)
          .eq('user_id', userId);

      if (!mounted) return;

      await _reload();

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Notification marked as read'),
          backgroundColor: Color(0xFF16A34A),
          duration: Duration(seconds: 2),
        ),
      );
    } catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Could not update notification: $e'),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _updatingIds.remove(notificationId);
        });
      }
    }
  }

  String _formatDate(dynamic value) {
    if (value == null) return '';

    final date = DateTime.tryParse(value.toString());

    if (date == null) return value.toString();

    final local = date.toLocal();

    String twoDigits(int number) => number.toString().padLeft(2, '0');

    return '${twoDigits(local.day)}/'
        '${twoDigits(local.month)}/'
        '${local.year}  '
        '${twoDigits(local.hour)}:'
        '${twoDigits(local.minute)}';
  }

  Widget _filterButton({
    required String label,
    required bool selected,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(25),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 10),
        decoration: BoxDecoration(
          color: selected ? blue : Colors.white,
          borderRadius: BorderRadius.circular(25),
          border: Border.all(color: selected ? blue : const Color(0xFFE2E8F0)),
        ),
        child: Text(
          label,
          style: TextStyle(
            color: selected ? Colors.white : muted,
            fontWeight: FontWeight.w600,
            fontSize: 13,
          ),
        ),
      ),
    );
  }

  Widget _notificationCard(Map<String, dynamic> item) {
    final isRead = item['is_read'] == true;
    final id = item['id'];
    final isUpdating = _updatingIds.contains(id?.toString());

    final title = item['title']?.toString() ?? 'Ticket Status Updated';

    final message = item['message']?.toString() ?? '';

    final date = _formatDate(item['created_at']);

    return Container(
      margin: const EdgeInsets.only(bottom: 13),
      decoration: BoxDecoration(
        color: isRead ? Colors.white : const Color(0xFFF0F6FF),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(
          color: isRead ? const Color(0xFFE7ECF3) : const Color(0xFFBFDBFE),
        ),
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          borderRadius: BorderRadius.circular(18),
          onTap: isRead || isUpdating ? null : () => _markAsRead(id),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: isRead
                        ? const Color(0xFFF1F5F9)
                        : const Color(0xFFDBEAFE),
                    borderRadius: BorderRadius.circular(13),
                  ),
                  child: Icon(
                    isRead
                        ? Icons.notifications_none
                        : Icons.notifications_active_outlined,
                    color: isRead ? muted : blue,
                    size: 24,
                  ),
                ),

                const SizedBox(width: 13),

                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              title,
                              style: TextStyle(
                                fontSize: 15,
                                fontWeight: isRead
                                    ? FontWeight.w600
                                    : FontWeight.bold,
                                color: navy,
                              ),
                            ),
                          ),

                          if (!isRead && !isUpdating)
                            Container(
                              width: 9,
                              height: 9,
                              decoration: const BoxDecoration(
                                color: blue,
                                shape: BoxShape.circle,
                              ),
                            ),

                          if (isUpdating)
                            const SizedBox(
                              width: 16,
                              height: 16,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            ),
                        ],
                      ),

                      const SizedBox(height: 8),

                      Text(
                        message,
                        style: const TextStyle(
                          fontSize: 13,
                          color: muted,
                          height: 1.5,
                        ),
                      ),

                      const SizedBox(height: 12),

                      Row(
                        children: [
                          const Icon(Icons.access_time, size: 14, color: muted),

                          const SizedBox(width: 5),

                          Expanded(
                            child: Text(
                              date,
                              style: const TextStyle(
                                fontSize: 11,
                                color: muted,
                              ),
                            ),
                          ),

                          if (!isRead)
                            const Text(
                              'Mark as read',
                              style: TextStyle(
                                fontSize: 11,
                                color: blue,
                                fontWeight: FontWeight.w600,
                              ),
                            )
                          else
                            const Icon(
                              Icons.check_circle_outline,
                              color: Color(0xFF16A34A),
                              size: 17,
                            ),
                        ],
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _emptyState({required bool unreadOnly}) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(25),
              decoration: const BoxDecoration(
                color: Color(0xFFEFF6FF),
                shape: BoxShape.circle,
              ),
              child: Icon(
                unreadOnly
                    ? Icons.mark_email_read_outlined
                    : Icons.notifications_none,
                size: 50,
                color: blue,
              ),
            ),

            const SizedBox(height: 20),

            Text(
              unreadOnly ? 'All Caught Up!' : 'No Notifications Yet',
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: navy,
              ),
            ),

            const SizedBox(height: 9),

            Text(
              unreadOnly
                  ? 'You have no unread notifications.'
                  : 'Updates about your IT support tickets will appear here.',
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 14, color: muted),
            ),

            const SizedBox(height: 22),

            OutlinedButton.icon(
              onPressed: _reload,
              icon: const Icon(Icons.refresh),
              label: const Text('Refresh'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _notificationsContent(List<Map<String, dynamic>> items) {
    final unreadCount = items.where((item) => item['is_read'] != true).length;

    final visibleItems = _showUnreadOnly
        ? items.where((item) => item['is_read'] != true).toList()
        : items;

    return Column(
      children: [
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(20),
          margin: const EdgeInsets.fromLTRB(18, 18, 18, 0),
          decoration: BoxDecoration(
            color: navy,
            borderRadius: BorderRadius.circular(20),
          ),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(13),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: const Icon(
                  Icons.notifications_outlined,
                  color: Colors.white,
                  size: 29,
                ),
              ),

              const SizedBox(width: 15),

              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Your Notifications',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 19,
                        fontWeight: FontWeight.bold,
                      ),
                    ),

                    const SizedBox(height: 5),

                    Text(
                      unreadCount == 0
                          ? 'You are all caught up!'
                          : '$unreadCount unread notification${unreadCount == 1 ? '' : 's'}',
                      style: const TextStyle(
                        color: Color(0xFFCBD5E1),
                        fontSize: 13,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),

        const SizedBox(height: 20),

        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 18),
          child: Row(
            children: [
              _filterButton(
                label: 'All (${items.length})',
                selected: !_showUnreadOnly,
                onTap: () {
                  setState(() {
                    _showUnreadOnly = false;
                  });
                },
              ),

              const SizedBox(width: 10),

              _filterButton(
                label: 'Unread ($unreadCount)',
                selected: _showUnreadOnly,
                onTap: () {
                  setState(() {
                    _showUnreadOnly = true;
                  });
                },
              ),
            ],
          ),
        ),

        const SizedBox(height: 18),

        Expanded(
          child: visibleItems.isEmpty
              ? _emptyState(unreadOnly: _showUnreadOnly)
              : RefreshIndicator(
                  onRefresh: _reload,
                  child: ListView.builder(
                    padding: const EdgeInsets.fromLTRB(18, 0, 18, 20),
                    itemCount: visibleItems.length,
                    itemBuilder: (context, index) {
                      return _notificationCard(visibleItems[index]);
                    },
                  ),
                ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: background,

      appBar: AppBar(
        backgroundColor: navy,
        foregroundColor: Colors.white,
        elevation: 0,
        title: const Text(
          'Notifications',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        actions: [
          IconButton(
            tooltip: 'Refresh Notifications',
            icon: const Icon(Icons.refresh),
            onPressed: _reload,
          ),
        ],
      ),

      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 850),
          child: FutureBuilder<List<Map<String, dynamic>>>(
            future: _notifications,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Center(child: CircularProgressIndicator());
              }

              if (snapshot.hasError) {
                return Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(
                          Icons.error_outline,
                          size: 55,
                          color: Colors.red,
                        ),

                        const SizedBox(height: 14),

                        Text(
                          'Unable to load notifications:\n${snapshot.error}',
                          textAlign: TextAlign.center,
                        ),

                        const SizedBox(height: 20),

                        ElevatedButton.icon(
                          onPressed: _reload,
                          icon: const Icon(Icons.refresh),
                          label: const Text('Try Again'),
                        ),
                      ],
                    ),
                  ),
                );
              }

              return _notificationsContent(snapshot.data ?? []);
            },
          ),
        ),
      ),

      bottomNavigationBar: const StaffBottomNav(selectedIndex: 0),
    );
  }
}
