// ===== СКЛАД ДЕТАЛЕЙ =====

function initWarehouseDetails() {
    $('#refresh-detail-stocks').off('click').on('click', loadDetailStocks);
    $('#refresh-detail-transactions').off('click').on('click', loadDetailTransactions);
    $('#add-detail-receipt-btn').off('click').on('click', () => showDetailMovementModal('receipt'));
    $('#add-detail-shipment-btn').off('click').on('click', () => showDetailMovementModal('shipment'));

    $('.warehouse-details-tab-btn').off('click').on('click', function() {
        switchWarehouseDetailsTab($(this).data('wd-tab'));
    });
}

function switchWarehouseDetailsTab(tab) {
    $('#warehouse-details .inventory-tab-content').css('display', '');

    $('.warehouse-details-tab-btn').removeClass('active');
    $(`.warehouse-details-tab-btn[data-wd-tab="${tab}"]`).addClass('active');
    $('#warehouse-details .warehouse-details-tab-content').removeClass('active');
    $(`#wd-${tab}-tab`).addClass('active');

    if (tab === 'stocks') loadDetailStocks();
    else if (tab === 'transactions') loadDetailTransactions();
}

async function loadDetailStocks() {
    try {
        const stocks = await apiRequest('GET', 'detailinventory/stocks');
        const tbody = $('#detail-stocks-table-body');
        tbody.empty();

        if (!stocks || stocks.length === 0) {
            tbody.append('<tr><td colspan="7" style="text-align:center">Нет данных. Добавьте приход деталей.</td></tr>');
            return;
        }

        stocks.forEach(s => {
            tbody.append(`
                <tr>
                    <td>${s.detailStockID}</td>
                    <td>${s.detailName || '-'}</td>
                    <td>${s.detailCode || '-'}</td>
                    <td><strong>${s.currentQuantity}</strong></td>
                    <td>${s.receivedQuantity}</td>
                    <td>${s.shippedQuantity}</td>
                    <td>${formatDetailDateTime(s.lastUpdated)}</td>
                </tr>
            `);
        });
    } catch (error) {
        console.error(error);
        showNotification('Ошибка загрузки остатков деталей', 'error');
    }
}

async function loadDetailTransactions() {
    try {
        const transactions = await apiRequest('GET', 'detailinventory/transactions');
        const tbody = $('#detail-transactions-table-body');
        tbody.empty();

        if (!transactions || transactions.length === 0) {
            tbody.append('<tr><td colspan="7" style="text-align:center">Нет операций</td></tr>');
            return;
        }

        const typeLabels = {
            Receipt: 'Приход',
            Shipment: 'Отгрузка',
            Production: 'Производство',
            ProductionCancel: 'Отмена производства',
            Adjustment: 'Корректировка'
        };

        transactions.forEach(t => {
            tbody.append(`
                <tr>
                    <td>${t.detailTransactionID}</td>
                    <td>${formatDetailDateTime(t.transactionDate)}</td>
                    <td>${t.detailName || '-'}</td>
                    <td>${typeLabels[t.transactionType] || t.transactionType}</td>
                    <td>${t.quantity > 0 ? '+' : ''}${t.quantity}</td>
                    <td>${t.documentNumber || '-'}</td>
                    <td>${t.description || '-'}</td>
                </tr>
            `);
        });
    } catch (error) {
        console.error(error);
        showNotification('Ошибка загрузки истории деталей', 'error');
    }
}

async function showDetailMovementModal(type) {
    const isReceipt = type === 'receipt';
    const title = isReceipt ? 'Приход деталей на склад' : 'Отгрузка деталей со склада';

    try {
        const details = await apiRequest('GET', 'Detail');
        const options = (details || []).map(d =>
            `<option value="${d.detailID}">${d.detailName} (${d.detailCode || d.detailID})</option>`
        ).join('');

        const content = `
            <form id="detail-movement-form">
                <div class="form-group">
                    <label for="dm-detail-id">Деталь *</label>
                    <select id="dm-detail-id" class="form-control" required>
                        <option value="">Выберите деталь</option>
                        ${options}
                    </select>
                </div>
                <div class="form-group">
                    <label for="dm-quantity">Количество (шт.) *</label>
                    <input type="number" id="dm-quantity" class="form-control" min="1" required>
                </div>
                <div class="form-group">
                    <label for="dm-document">Номер документа</label>
                    <input type="number" id="dm-document" class="form-control">
                </div>
                <div class="form-group">
                    <label for="dm-description">Примечание</label>
                    <textarea id="dm-description" class="form-control" rows="2"></textarea>
                </div>
                <div class="form-actions">
                    <button type="submit" class="btn btn-primary">Сохранить</button>
                    <button type="button" class="btn btn-secondary" onclick="closeModal()">Отмена</button>
                </div>
            </form>
        `;

        showModal(title, content);

        $('#detail-movement-form').submit(async function(e) {
            e.preventDefault();

            const detailId = parseInt($('#dm-detail-id').val());
            const quantity = parseInt($('#dm-quantity').val());

            if (!detailId || !quantity || quantity <= 0) {
                showNotification('Заполните все обязательные поля', 'warning');
                return;
            }

            const payload = {
                detailId,
                quantity,
                documentNumber: $('#dm-document').val() ? parseInt($('#dm-document').val()) : null,
                description: $('#dm-description').val() || null
            };

            try {
                await apiRequest('POST', `detailinventory/${isReceipt ? 'receipt' : 'shipment'}`, payload);
                showNotification(isReceipt ? 'Приход зарегистрирован' : 'Отгрузка зарегистрирована', 'success');
                closeModal();
                loadDetailStocks();
                loadDetailTransactions();
            } catch (error) {
                showNotification('Ошибка сохранения операции', 'error');
            }
        });
    } catch (error) {
        showNotification('Не удалось загрузить список деталей', 'error');
    }
}

function formatDetailDateTime(dateStr) {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleString('ru-RU');
}
