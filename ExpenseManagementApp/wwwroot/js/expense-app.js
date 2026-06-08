const errorBanner = document.getElementById("error-banner");
const userSelect = document.getElementById("user-select");
const categorySelect = document.getElementById("category-select");
const statusFilter = document.getElementById("status-filter");
const expensesTableBody = document.querySelector("#expenses-table tbody");
const createExpenseForm = document.getElementById("create-expense-form");
const refreshBtn = document.getElementById("refresh-btn");

const showError = (text) => {
  if (!text) {
    errorBanner.classList.add("d-none");
    errorBanner.innerText = "";
    return;
  }
  errorBanner.classList.remove("d-none");
  errorBanner.innerText = text;
};

const loadLookups = async () => {
  const [usersRes, catsRes] = await Promise.all([
    fetch("/api/lookups/users"),
    fetch("/api/lookups/categories")
  ]);
  const users = await usersRes.json();
  const cats = await catsRes.json();
  showError(users.errorHeader || cats.errorHeader || null);

  userSelect.innerHTML = "";
  (users.data || []).forEach((x) => {
    const option = document.createElement("option");
    option.value = x.email;
    option.textContent = `${x.userName} (${x.email})`;
    userSelect.append(option);
  });

  categorySelect.innerHTML = "";
  (cats.data || []).forEach((x) => {
    const option = document.createElement("option");
    option.value = x.categoryName;
    option.textContent = x.categoryName;
    categorySelect.append(option);
  });
};

const loadExpenses = async () => {
  const status = encodeURIComponent(statusFilter.value || "");
  const response = await fetch(`/api/expenses?status=${status}`);
  const payload = await response.json();

  showError(payload.errorHeader || null);
  expensesTableBody.innerHTML = "";

  (payload.data || []).forEach((item) => {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${item.expenseId}</td>
      <td>${item.userName}</td>
      <td>${item.category}</td>
      <td>${item.status}</td>
      <td>£${item.amount.toFixed(2)}</td>
      <td>${item.expenseDate}</td>
      <td>${item.description ?? ""}</td>
      <td>
        <button class="btn btn-sm btn-success" data-action="Approved" data-id="${item.expenseId}">Approve</button>
        <button class="btn btn-sm btn-outline-danger ms-2" data-action="Rejected" data-id="${item.expenseId}">Reject</button>
      </td>`;
    expensesTableBody.append(tr);
  });
};

const updateExpenseStatus = async (expenseId, statusName) => {
  const managerEmail = "bob.manager@example.co.uk";
  const response = await fetch(`/api/expenses/${expenseId}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ managerEmail, statusName })
  });
  const payload = await response.json();
  showError(payload.errorHeader || null);
  await loadExpenses();
};

createExpenseForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const request = {
    userEmail: userSelect.value,
    categoryName: categorySelect.value,
    amount: Number(document.getElementById("amount-input").value),
    expenseDate: document.getElementById("date-input").value,
    description: document.getElementById("description-input").value
  };

  const response = await fetch("/api/expenses", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request)
  });

  const payload = await response.json();
  showError(payload.errorHeader || null);
  if (response.ok) {
    createExpenseForm.reset();
    await loadExpenses();
  }
});

expensesTableBody.addEventListener("click", async (event) => {
  const target = event.target;
  if (!(target instanceof HTMLElement)) return;
  const id = target.getAttribute("data-id");
  const action = target.getAttribute("data-action");
  if (!id || !action) return;
  await updateExpenseStatus(Number(id), action);
});

refreshBtn.addEventListener("click", loadExpenses);
statusFilter.addEventListener("change", loadExpenses);

(async () => {
  await loadLookups();
  await loadExpenses();
})();
