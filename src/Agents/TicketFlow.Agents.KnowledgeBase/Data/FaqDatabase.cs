namespace TicketFlow.Agents.KnowledgeBase.Data;

public static class FaqDatabase
{
    public static readonly List<FaqEntry> Entries =
    [
        new FaqEntry
        {
            Id = "faq-001",
            Question = "Jak zresetować hasło?",
            Answer = """
                Aby zresetować hasło:
                1. Kliknij "Zapomniałeś hasła?" na stronie logowania
                2. Wpisz swój email
                3. Sprawdź skrzynkę pocztową (może być w spam)
                4. Kliknij link w emailu (ważny 24h)
                5. Ustaw nowe hasło (min. 8 znaków, wielka litera, cyfra)
                """,
            Keywords = ["hasło", "password", "reset", "zapomniałem", "forgot"],
            Category = "Authentication",
            Priority = 10
        },

        new FaqEntry
        {
            Id = "faq-002",
            Question = "Jak zmienić adres email?",
            Answer = """
                Zmiana adresu email:
                1. Zaloguj się na konto
                2. Przejdź do Ustawienia → Profil
                3. Kliknij "Zmień email"
                4. Wpisz nowy email i potwierdź hasłem
                5. Potwierdź zmianę z linku wysłanego na STARY email
                6. Aktywuj nowy email z linku wysłanego na NOWY email
                """,
            Keywords = ["email", "zmiana", "adres", "change", "update"],
            Category = "Account",
            Priority = 8
        },

        new FaqEntry
        {
            Id = "faq-003",
            Question = "Jak dodać nowy ticket?",
            Answer = """
                Tworzenie nowego ticketu:
                1. Zaloguj się na konto
                2. Kliknij "Nowy Ticket" (przycisk + w prawym górnym rogu)
                3. Wybierz kategorię (Techniczny/Billing/Ogólny)
                4. Wpisz tytuł i opis problemu
                5. Opcjonalnie dodaj załączniki (max 10MB)
                6. Kliknij "Wyślij"

                Otrzymasz potwierdzenie na email i możesz śledzić status.
                """,
            Keywords = ["ticket", "nowy", "utworzyć", "dodać", "create", "new", "zgłoszenie"],
            Category = "Tickets",
            Priority = 9
        },

        new FaqEntry
        {
            Id = "faq-004",
            Question = "Jak sprawdzić status ticketu?",
            Answer = """
                Sprawdzanie statusu ticketu:
                1. Zaloguj się
                2. Przejdź do "Moje Tickety"
                3. Zobacz listę wszystkich ticketów z aktualnym statusem
                4. Kliknij na ticket aby zobaczyć szczegóły

                Statusy:
                - Nowy: Czeka na przypisanie
                - W trakcie: Agent pracuje nad rozwiązaniem
                - Oczekuje: Czekamy na twoją odpowiedź
                - Rozwiązany: Problem został rozwiązany
                """,
            Keywords = ["status", "ticket", "sprawdzić", "check", "track", "zgłoszenie"],
            Category = "Tickets",
            Priority = 10
        },

        new FaqEntry
        {
            Id = "faq-005",
            Question = "Jak anulować subskrypcję?",
            Answer = """
                Anulowanie subskrypcji:
                1. Zaloguj się
                2. Ustawienia → Billing
                3. Kliknij "Zarządzaj subskrypcją"
                4. Wybierz "Anuluj subskrypcję"
                5. Podaj powód (opcjonalnie)
                6. Potwierdź

                WAŻNE: Zachowasz dostęp do końca opłaconego okresu.
                Dane pozostaną przez 30 dni gdybyś chciał wrócić.
                """,
            Keywords = ["anulować", "subskrypcja", "cancel", "subscription", "rezygnacja"],
            Category = "Billing",
            Priority = 7
        },

        new FaqEntry
        {
            Id = "faq-006",
            Question = "Jak zmienić plan subskrypcji?",
            Answer = """
                Zmiana planu:
                1. Zaloguj się
                2. Ustawienia → Billing
                3. Kliknij "Zmień plan"
                4. Wybierz nowy plan (Basic/Pro/Enterprise)
                5. Potwierdź zmianę

                Przy upgrade: różnica rozliczana proporcjonalnie
                Przy downgrade: zmiana od następnego okresu rozliczeniowego
                """,
            Keywords = ["plan", "zmiana", "upgrade", "downgrade", "subskrypcja"],
            Category = "Billing",
            Priority = 6
        },

        new FaqEntry
        {
            Id = "faq-007",
            Question = "Jak dodać nowego użytkownika do konta firmowego?",
            Answer = """
                Dodawanie użytkownika (wymaga roli Admin):
                1. Zaloguj się jako Admin
                2. Ustawienia → Użytkownicy
                3. Kliknij "Dodaj użytkownika"
                4. Wpisz email nowego użytkownika
                5. Wybierz rolę (Agent/Admin/Viewer)
                6. Kliknij "Wyślij zaproszenie"

                Użytkownik otrzyma email z linkiem do aktywacji.
                """,
            Keywords = ["użytkownik", "dodać", "add", "user", "konto", "zespół", "team"],
            Category = "Account",
            Priority = 5
        },

        new FaqEntry
        {
            Id = "faq-008",
            Question = "Jak włączyć powiadomienia?",
            Answer = """
                Konfiguracja powiadomień:
                1. Zaloguj się
                2. Ustawienia → Powiadomienia
                3. Wybierz które powiadomienia chcesz otrzymywać:
                   - Email: Nowy ticket, zmiana statusu
                   - Push: Pilne tickety, eskalacje
                   - Slack: Integracja z workspace
                4. Zapisz zmiany
                """,
            Keywords = ["powiadomienia", "notifications", "email", "push", "alert"],
            Category = "Settings",
            Priority = 5
        },

        new FaqEntry
        {
            Id = "faq-009",
            Question = "Jak skonfigurować integrację ze Slack?",
            Answer = """
                Integracja Slack:
                1. Zaloguj się (wymaga roli Admin)
                2. Ustawienia → Integracje
                3. Kliknij "Dodaj Slack"
                4. Autoryzuj dostęp do workspace
                5. Wybierz kanał dla powiadomień
                6. Skonfiguruj jakie zdarzenia mają być wysyłane

                Dostępne zdarzenia: nowy ticket, eskalacja, SLA alert
                """,
            Keywords = ["slack", "integracja", "integration", "webhook"],
            Category = "Integrations",
            Priority = 4
        },

        new FaqEntry
        {
            Id = "faq-010",
            Question = "Jak eskalować ticket?",
            Answer = """
                Eskalacja ticketu:
                1. Otwórz szczegóły ticketu
                2. Kliknij "Eskaluj" (górny prawy róg)
                3. Wybierz powód eskalacji
                4. Opcjonalnie dodaj komentarz
                5. Potwierdź

                Ticket zostanie przypisany do specjalisty lub supervisora.
                Możesz też napisać na czacie - nasz agent pomoże z eskalacją.
                """,
            Keywords = ["eskalacja", "escalate", "pilne", "urgent", "supervisor"],
            Category = "Tickets",
            Priority = 8
        },

        new FaqEntry
        {
            Id = "faq-011",
            Question = "Jak zmienić priorytet ticketu?",
            Answer = """
                Zmiana priorytetu:
                1. Otwórz szczegóły ticketu
                2. W sekcji "Szczegóły" kliknij na priorytet
                3. Wybierz nowy priorytet:
                   - Low: Odpowiedź w 48h
                   - Medium: Odpowiedź w 24h
                   - High: Odpowiedź w 4h
                   - Critical: Odpowiedź w 1h
                4. Zapisz

                UWAGA: Zmiana priorytetu może wymagać weryfikacji.
                """,
            Keywords = ["priorytet", "priority", "zmiana", "urgency", "pilność"],
            Category = "Tickets",
            Priority = 6
        },

        new FaqEntry
        {
            Id = "faq-012",
            Question = "Jak pobrać fakturę?",
            Answer = """
                Pobieranie faktury:
                1. Zaloguj się
                2. Ustawienia → Billing → Historia płatności
                3. Znajdź fakturę na liście
                4. Kliknij ikonę pobierania (PDF)

                Faktury wysyłamy też automatycznie na email po każdej płatności.
                Potrzebujesz faktury za inny okres? Skontaktuj się z nami.
                """,
            Keywords = ["faktura", "invoice", "pobierz", "download", "pdf", "billing"],
            Category = "Billing",
            Priority = 6
        },

        new FaqEntry
        {
            Id = "faq-013",
            Question = "Jak zmienić dane do faktury?",
            Answer = """
                Zmiana danych fakturowych:
                1. Zaloguj się
                2. Ustawienia → Billing → Dane firmy
                3. Edytuj:
                   - Nazwa firmy
                   - NIP
                   - Adres
                4. Zapisz zmiany

                Zmiany obowiązują od następnej faktury.
                Nie możemy modyfikować już wystawionych faktur.
                """,
            Keywords = ["faktura", "dane", "firma", "nip", "adres", "invoice"],
            Category = "Billing",
            Priority = 5
        },

        new FaqEntry
        {
            Id = "faq-014",
            Question = "Jak włączyć dwustopniową weryfikację (2FA)?",
            Answer = """
                Włączanie 2FA:
                1. Zaloguj się
                2. Ustawienia → Bezpieczeństwo
                3. Kliknij "Włącz 2FA"
                4. Zeskanuj kod QR aplikacją (Google Authenticator, Authy)
                5. Wpisz kod weryfikacyjny
                6. Zapisz kody zapasowe (jednorazowe)

                WAŻNE: Kody zapasowe pozwolą odzyskać dostęp jeśli stracisz telefon.
                """,
            Keywords = ["2fa", "dwustopniowa", "weryfikacja", "bezpieczeństwo", "security", "authenticator"],
            Category = "Security",
            Priority = 7
        },

        new FaqEntry
        {
            Id = "faq-015",
            Question = "Jak eksportować dane z systemu?",
            Answer = """
                Eksport danych:
                1. Zaloguj się (wymaga roli Admin)
                2. Ustawienia → Dane → Eksport
                3. Wybierz zakres danych:
                   - Wszystkie tickety
                   - Tickety z okresu
                   - Statystyki
                4. Wybierz format (CSV, JSON, Excel)
                5. Kliknij "Eksportuj"

                Link do pobrania zostanie wysłany na email (duże pliki).
                """,
            Keywords = ["eksport", "export", "dane", "data", "csv", "excel", "backup"],
            Category = "Data",
            Priority = 4
        },

        new FaqEntry
        {
            Id = "faq-016",
            Question = "Jak usunąć konto?",
            Answer = """
                Usunięcie konta:
                1. Zaloguj się
                2. Ustawienia → Konto → Usuń konto
                3. Wpisz hasło dla potwierdzenia
                4. Przeczytaj informacje o konsekwencjach
                5. Potwierdź usunięcie

                UWAGA:
                - Dane zostaną usunięte bezpowrotnie po 30 dniach
                - Aktywna subskrypcja zostanie anulowana
                - Możesz odwołać usunięcie w ciągu 30 dni kontaktując się z supportem
                """,
            Keywords = ["usunięcie", "konto", "delete", "account", "kasowanie"],
            Category = "Account",
            Priority = 3
        },

        new FaqEntry
        {
            Id = "faq-017",
            Question = "Jak przypisać ticket do innego agenta?",
            Answer = """
                Przypisanie ticketu (wymaga roli Agent/Admin):
                1. Otwórz szczegóły ticketu
                2. W sekcji "Przypisanie" kliknij "Zmień"
                3. Wyszukaj agenta po imieniu lub emailu
                4. Wybierz agenta z listy
                5. Opcjonalnie dodaj komentarz
                6. Zapisz

                Agent otrzyma powiadomienie o nowym przypisaniu.
                """,
            Keywords = ["przypisanie", "assign", "agent", "transfer", "przekazanie"],
            Category = "Tickets",
            Priority = 6
        },

        new FaqEntry
        {
            Id = "faq-018",
            Question = "Jak działa SLA?",
            Answer = """
                SLA (Service Level Agreement):

                Czas reakcji (pierwsza odpowiedź):
                - Critical: 1 godzina
                - High: 4 godziny
                - Medium: 24 godziny
                - Low: 48 godzin

                Czas rozwiązania:
                - Critical: 4 godziny
                - High: 1 dzień roboczy
                - Medium: 3 dni robocze
                - Low: 5 dni roboczych

                Godziny pracy: Pon-Pt 8:00-18:00 CET
                Tickety Critical obsługiwane 24/7
                """,
            Keywords = ["sla", "czas", "odpowiedź", "reakcja", "response", "time"],
            Category = "Support",
            Priority = 7
        },

        new FaqEntry
        {
            Id = "faq-019",
            Question = "Jak dodać załącznik do ticketu?",
            Answer = """
                Dodawanie załącznika:
                1. Otwórz szczegóły ticketu
                2. W polu odpowiedzi kliknij ikonę spinacza
                3. Wybierz plik (lub przeciągnij i upuść)
                4. Poczekaj na upload
                5. Wyślij odpowiedź

                Limity:
                - Max rozmiar pliku: 10MB
                - Dozwolone formaty: PDF, DOC, DOCX, XLS, XLSX, PNG, JPG, GIF, ZIP
                - Max 5 załączników na wiadomość
                """,
            Keywords = ["załącznik", "attachment", "plik", "file", "upload", "zdjęcie", "screenshot"],
            Category = "Tickets",
            Priority = 5
        },

        new FaqEntry
        {
            Id = "faq-020",
            Question = "Co zrobić gdy otrzymuję błąd 500?",
            Answer = """
                Błąd 500 (Internal Server Error):
                1. Odśwież stronę (Ctrl+F5 lub Cmd+Shift+R)
                2. Wyczyść cache przeglądarki
                3. Sprawdź https://status.ticketflow.com czy są problemy
                4. Jeśli problem się powtarza, utwórz ticket

                W tickecie podaj:
                - Dokładny czas wystąpienia błędu
                - Stronę na której wystąpił
                - Screenshot komunikatu (jeśli możliwe)
                - Przeglądarkę i wersję
                """,
            Keywords = ["błąd", "error", "500", "internal", "server", "problem"],
            Category = "Technical",
            Priority = 6
        }
    ];
}
