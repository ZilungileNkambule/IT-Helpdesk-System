import 'package:flutter/material.dart';

import '../core/theme/app_theme.dart';
import '../widgets/staff_bottom_nav.dart';
import 'staff_dashboard_screen.dart';

class HelpSupportScreen extends StatelessWidget {
  const HelpSupportScreen({super.key});

  static const Color navy = AppTheme.navy;
  static const Color blue = AppTheme.blue;
  static const Color background = Color(0xFFF5F7FB);
  static const Color muted = Color(0xFF64748B);

  void _openReport(BuildContext context) {
    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(
        builder: (_) => const StaffDashboardScreen(initialPage: 2),
      ),
      (route) => false,
    );
  }

  Widget _helpCard({
    required IconData icon,
    required String title,
    required String description,
  }) {
    return Container(
      margin: const EdgeInsets.only(bottom: 13),
      padding: const EdgeInsets.all(17),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: const Color(0xFFEFF6FF),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(icon, color: blue, size: 24),
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
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 7),
                Text(
                  description,
                  style: const TextStyle(
                    color: muted,
                    fontSize: 13,
                    height: 1.5,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _faqItem({required String question, required String answer}) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFE2E8F0)),
      ),
      child: ExpansionTile(
        shape: const Border(),
        collapsedShape: const Border(),
        iconColor: blue,
        collapsedIconColor: muted,
        title: Text(
          question,
          style: const TextStyle(
            color: navy,
            fontSize: 14,
            fontWeight: FontWeight.w600,
          ),
        ),
        childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 18),
        expandedAlignment: Alignment.centerLeft,
        children: [
          Text(
            answer,
            style: const TextStyle(color: muted, fontSize: 13, height: 1.5),
          ),
        ],
      ),
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
          'Help & Support',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
      ),

      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 750),
          child: ListView(
            padding: const EdgeInsets.all(18),
            children: [
              // Navy welcome banner
              Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: navy,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: const Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(Icons.support_agent, color: Colors.white, size: 42),
                    SizedBox(height: 15),
                    Text(
                      'How can we help you?',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 23,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    SizedBox(height: 8),
                    Text(
                      'Find answers and learn how to use the IT Helpdesk system.',
                      style: TextStyle(
                        color: Color(0xFFCBD5E1),
                        fontSize: 14,
                        height: 1.5,
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 26),

              const Text(
                'Getting Started',
                style: TextStyle(
                  color: navy,
                  fontSize: 19,
                  fontWeight: FontWeight.bold,
                ),
              ),

              const SizedBox(height: 14),

              _helpCard(
                icon: Icons.add_circle_outline,
                title: 'Report an IT Issue',
                description:
                    'Submit a support request by selecting an issue category and priority.',
              ),

              _helpCard(
                icon: Icons.list_alt_outlined,
                title: 'Track Your Tickets',
                description:
                    'View all your submitted tickets and monitor their progress.',
              ),

              _helpCard(
                icon: Icons.notifications_outlined,
                title: 'Stay Updated',
                description:
                    'Receive notifications when the status of your ticket changes.',
              ),

              const SizedBox(height: 20),

              const Text(
                'Frequently Asked Questions',
                style: TextStyle(
                  color: navy,
                  fontSize: 19,
                  fontWeight: FontWeight.bold,
                ),
              ),

              const SizedBox(height: 14),

              _faqItem(
                question: 'How do I create a ticket?',
                answer:
                    'Tap Report in the bottom navigation bar, select the issue category, choose Low, Medium or High priority, and tap Submit Ticket.',
              ),

              _faqItem(
                question: 'How do I check my ticket status?',
                answer:
                    'Open My Tickets from the bottom navigation. Select a ticket to view its status, priority and any resolution notes.',
              ),

              _faqItem(
                question: 'What do the ticket statuses mean?',
                answer:
                    'Open means the request has been submitted. In Progress means it is being worked on. Resolved means a solution has been recorded. Closed means the ticket has been completed.',
              ),

              _faqItem(
                question: 'How will I know when my ticket is updated?',
                answer:
                    'Open Notifications from your dashboard or profile. Ticket updates appear there when the system creates a notification.',
              ),

              _faqItem(
                question: 'How do I change my password?',
                answer:
                    'Open Profile, select Change Password, and follow the instructions to update your account password.',
              ),

              _faqItem(
                question: 'Can I assign tickets to technicians?',
                answer:
                    'No. Staff members submit and track tickets. Ticket assignment is managed through the web system by authorized managers.',
              ),

              const SizedBox(height: 20),

              Container(
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: const Color(0xFFEFF6FF),
                  borderRadius: BorderRadius.circular(17),
                  border: Border.all(color: const Color(0xFFBFDBFE)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Icon(
                      Icons.headset_mic_outlined,
                      color: blue,
                      size: 31,
                    ),

                    const SizedBox(height: 12),

                    const Text(
                      'Still need assistance?',
                      style: TextStyle(
                        color: navy,
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),

                    const SizedBox(height: 8),

                    const Text(
                      'If you are experiencing a technical issue, submit a ticket so the IT team can assist you.',
                      style: TextStyle(color: muted, fontSize: 13, height: 1.5),
                    ),

                    const SizedBox(height: 17),

                    SizedBox(
                      width: double.infinity,
                      height: 50,
                      child: ElevatedButton.icon(
                        onPressed: () => _openReport(context),
                        icon: const Icon(Icons.add_circle_outline),
                        label: const Text('Report an Issue'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: blue,
                          foregroundColor: Colors.white,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 25),

              const Center(
                child: Text(
                  'IT Helpdesk • Staff Support Portal',
                  style: TextStyle(color: muted, fontSize: 12),
                ),
              ),

              const SizedBox(height: 15),
            ],
          ),
        ),
      ),

      bottomNavigationBar: const StaffBottomNav(selectedIndex: 0),
    );
  }
}
